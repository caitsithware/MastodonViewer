using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEditor;

namespace MastodonViewer
{
	public class MastodonViewerWindow : EditorWindow
	{
		private static readonly string s_URL = @"https://unityjp-mastodon.tokyo/api/v1/timelines/public?local=true";
		const double s_UpdateInterval = 10 * 60;
		const double s_RepaintInterval = 1 * 60;

		[MenuItem( "Window/Mastodon Viewer" )]
		static void Open()
		{
			GetWindow<MastodonViewerWindow>();
		}

		[Serializable]
		public class Account
		{
			public int id;
			public string username;
			public string display_name;
			public string avatar;

			public Texture2D avatarTexture
			{
				get
				{
					return AvatarManager.instance.GetAvatarTexture( avatar );
				}
			}
		}

		[Serializable]
		public class Status
		{
			public int id;
			public string created_at;
			public Account account;
			public string content;
			public string url;

			[NonSerialized]
			private bool m_InitializedCreatedAt = false;
			private DateTime m_CreatedAt;
			
			void InitializeDate()
			{
				if( !m_InitializedCreatedAt )
				{
					m_CreatedAt = DateTime.Parse( created_at );

					m_InitializedCreatedAt = true;
				}
			}

			public DateTime createdAt
			{
				get
				{
					InitializeDate();

					return m_CreatedAt;
				}
			}

			public TimeSpan span
			{
				get
				{
					return DateTime.Now - createdAt;
				}
			}
		}

		[Serializable]
		public class Timeline
		{
			public List<Status> statuses;
		}

		private WWW m_WWWTimelines = null;
		private Timeline m_TimeLine = null;
		private Vector2 m_ScrollPos;
		private double m_UpdateTimer;
		private double m_RepaintTimer;

		private void OnEnable()
		{
			titleContent = new GUIContent( "Mastdon" );
			m_UpdateTimer = EditorApplication.timeSinceStartup;
			m_RepaintTimer = EditorApplication.timeSinceStartup;

			UpdateTimeline();
		}

		private void OnInspectorUpdate()
		{
			if( EditorApplication.timeSinceStartup - m_UpdateTimer >= s_UpdateInterval )
			{
				UpdateTimeline();
			}

			bool repaint = false;

			if( m_WWWTimelines != null && m_WWWTimelines.isDone )
			{
				if( m_TimeLine == null )
				{
					m_TimeLine = JsonUtility.FromJson<Timeline>( "{ \"statuses\": " + m_WWWTimelines.text + " }" );
				}
				else
				{
					Timeline timeline = JsonUtility.FromJson<Timeline>( "{ \"statuses\": " + m_WWWTimelines.text + " }" );

					m_TimeLine.statuses.InsertRange( 0, timeline.statuses );
				}

				m_WWWTimelines.Dispose();

				m_WWWTimelines = null;

				m_UpdateTimer = EditorApplication.timeSinceStartup;

				repaint = true;
			}

			if( AvatarManager.instance.UpdateAvatar() )
			{
				repaint = true;
			}

			if( EditorApplication.timeSinceStartup - m_RepaintTimer >= s_RepaintInterval )
			{
				repaint = true;
			}

			if( repaint )
			{
				Repaint();
			}
		}

		void UpdateTimeline()
		{
			if( m_WWWTimelines == null )
			{
				string url = s_URL;
				if( m_TimeLine != null )
				{
					url += "&since_id=" + m_TimeLine.statuses[0].id;
				}
				m_WWWTimelines = new WWW( url );
			}
		}

		void DrawToolbar()
		{
			using( new EditorGUILayout.HorizontalScope( EditorStyles.toolbar, GUILayout.ExpandWidth(true) ) )
			{
				if( m_WWWTimelines == null )
				{
					if( GUILayout.Button( "更新", EditorStyles.toolbarButton,GUILayout.ExpandWidth(false) ) )
					{
						UpdateTimeline();
					}
				}
				else
				{
					GUILayout.Label( "読み込み中" );
				}
			}
		}

		void DrawTimeline()
		{
			if( m_TimeLine == null )
			{
				return;
			}
				
			using( EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope( m_ScrollPos ) )
			{
				m_ScrollPos = scroll.scrollPosition;
				int index = 0;

				foreach( Status status in m_TimeLine.statuses )
				{
					GUIStyle style = ( index % 2 == 0 ) ? Styles.entryBackEven : Styles.entryBackOdd;
					using( EditorGUILayout.VerticalScope virticalScope = new EditorGUILayout.VerticalScope( style ) )
					{
						using( new EditorGUILayout.HorizontalScope() )
						{
							GUILayout.Label( status.account.avatarTexture, GUILayout.Width( 48 ), GUILayout.Height( 48 ) );

							using( new EditorGUILayout.VerticalScope() )
							{
								using( new EditorGUILayout.HorizontalScope() )
								{
									string displayName = status.account.display_name;
									if( string.IsNullOrEmpty( displayName ) )
									{
										displayName = status.account.username;
									}
									GUILayout.Label( displayName, Styles.displayNameLabel, GUILayout.ExpandWidth( false ) );
									GUILayout.Label( "@" + status.account.username, Styles.userNameLabel, GUILayout.ExpandWidth( false ) );

									GUILayout.FlexibleSpace();

									string timestamp = string.Empty;
									TimeSpan span = status.span;

									if( span.TotalDays >= 1.0 )
									{
										timestamp = status.createdAt.ToString( "d" );
									}
									else if( span.Hours >= 1 )
									{
										timestamp = span.Hours + "時間前";
									}
									else if( span.Minutes >= 1 )
									{
										timestamp = span.Minutes + "分前";
									}
									else
									{
										timestamp = span.Seconds + "秒前";
									}

									GUILayout.Label( timestamp, Styles.timestampLabel, GUILayout.ExpandWidth( false ) );
								}

								XmlDocument xmlDoc = new XmlDocument();
								xmlDoc.XmlResolver = null;

								string content = status.content.Replace( "<br>", "<br />" );

								xmlDoc.LoadXml( "<content>" + content + "</content>" );

								foreach( XmlNode brNode in xmlDoc.GetElementsByTagName( "br" ) )
								{
									XmlNode n = xmlDoc.CreateTextNode( "\n" );
									brNode.ParentNode.ReplaceChild( n, brNode );
								}

								foreach( XmlNode pNode in xmlDoc.GetElementsByTagName( "p" ) )
								{
									XmlNode n = xmlDoc.CreateTextNode( pNode.InnerText + "\n" );
									pNode.ParentNode.ReplaceChild( n, pNode );
								}

								EditorGUILayout.LabelField( xmlDoc.InnerText, EditorStyles.wordWrappedLabel );
							}
						}

						EditorGUILayout.Separator();

						Event current = Event.current;
						if( current.type == EventType.MouseDown && virticalScope.rect.Contains( current.mousePosition ) )
						{
							Application.OpenURL( status.url );
						}
					}

					index++;
				}
			}
		}

		private void OnGUI()
		{
			DrawToolbar();
			DrawTimeline();

			m_RepaintTimer = EditorApplication.timeSinceStartup;
		}
	}
}
