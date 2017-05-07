using System;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace MastodonViewer
{
	public class MastodonViewerWindow : EditorWindow
	{
		private static readonly string s_URL = @"https://unityjp-mastodon.tokyo/api/v1/timelines/public?local=true";
		private static readonly System.Globalization.CultureInfo s_CalcureInfo = new System.Globalization.CultureInfo( "ja-JP" );
		const double s_UpdateInterval = 10 * 60;
		const double s_RepaintInterval = 1 * 60;
		const double s_APIAccessInterval = 10;

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
					return MediaManager.instance.GetMediaTexture( avatar );
				}
			}
		}

		[Serializable]
		public class Attachment
		{
			public string type;
			public string url;
			public string remote_url;
			public string preview_url;
			public string text_url;

			public Texture2D previewTexture
			{
				get
				{
					if( type != "image" )
					{
						return null;
					}

					return MediaManager.instance.GetMediaTexture( preview_url );
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
			public List<Attachment> media_attachments = new List<Attachment>();

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

			private string m_Text;
			public string text
			{
				get
				{
					if( string.IsNullOrEmpty( m_Text ) )
					{
						XmlDocument xmlDoc = new XmlDocument();
						xmlDoc.XmlResolver = null;

						string content = this.content.Replace( "<br>", "<br />" );

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

						m_Text = xmlDoc.InnerText;

						foreach( Attachment attachment in media_attachments )
						{
							if( attachment.type == "image" && !string.IsNullOrEmpty( attachment.text_url ) )
							{
								string pattern = string.Format( @"\b{0}\b", Regex.Escape( attachment.text_url ) );

								m_Text = Regex.Replace( m_Text, pattern, "", RegexOptions.Multiline );
							}
						}
					}

					return m_Text;
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
		private bool m_Back = false;
		private Vector2 m_ScrollPos;
		private double m_UpdateTimer;
		private double m_RepaintTimer;
		private double m_APIAccessTimer;

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

					if( m_Back )
					{
						m_TimeLine.statuses.AddRange( timeline.statuses );
					}
					else
					{
						m_TimeLine.statuses.InsertRange( 0, timeline.statuses );
					}
				}

				m_WWWTimelines.Dispose();

				m_WWWTimelines = null;

				m_UpdateTimer = EditorApplication.timeSinceStartup;

				repaint = true;
			}

			if( MediaManager.instance.UpdateMedia() )
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

		bool IsAPIAccess
		{
			get
			{
				return EditorApplication.timeSinceStartup - m_APIAccessTimer >= s_APIAccessInterval;
			}
		}

		void UpdateTimeline(bool clear = false,bool back=false)
		{
			if( m_WWWTimelines != null )
			{
				return;
			}

			if( !IsAPIAccess )
			{
				return;
			}

			m_APIAccessTimer = EditorApplication.timeSinceStartup;

			if( clear )
			{
				m_TimeLine = null;
			}

			string url = s_URL;
			if( m_TimeLine != null )
			{
				int count = m_TimeLine.statuses.Count;
				if( count > 0 )
				{
					if( back )
					{
						url += "&max_id=" + m_TimeLine.statuses[count-1].id;
					}
					else
					{
						url += "&since_id=" + m_TimeLine.statuses[0].id;
					}
					m_Back = back;
				}					
			}
			m_WWWTimelines = new WWW( url );
		}

		void DrawToolbar()
		{
			using( new EditorGUILayout.HorizontalScope( EditorStyles.toolbar, GUILayout.ExpandWidth(true) ) )
			{
				if( m_WWWTimelines == null )
				{
					using( new EditorGUI.DisabledGroupScope( !IsAPIAccess ) )
					{
						if( GUILayout.Button( "更新", EditorStyles.toolbarButton, GUILayout.ExpandWidth( false ) ) )
						{
							UpdateTimeline( true );
						}
					}
				}
				else
				{
					GUILayout.Label( "読み込み中" );
				}
			}
		}

		void DrawAttachment( Attachment attachment )
		{
			Texture2D texture = attachment.previewTexture;
			if( texture == null )
			{
				return;
			}

			Rect position = GUILayoutUtility.GetRect( 0, 110 );

			int controlId = GUIUtility.GetControlID( FocusType.Passive, position );

			Event current = Event.current;

			EventType eventType = current.GetTypeForControl( controlId );

			switch( eventType )
			{
				case EventType.MouseDown:
					if( position.Contains( current.mousePosition ) )
					{
						Application.OpenURL( attachment.url );

						current.Use();
					}
					break;
				case EventType.Repaint:
					if( position.Contains( current.mousePosition ) )
					{
						EditorGUIUtility.AddCursorRect( position, MouseCursor.Link );
					}
					EditorGUI.DrawPreviewTexture( position, texture, null, ScaleMode.ScaleAndCrop );
					break;
			}
		}

		void DrawTimeline()
		{
			if( m_TimeLine == null )
			{
				return;
			}

			using( EditorGUILayout.VerticalScope timelineAreaScope = new EditorGUILayout.VerticalScope() )
			{
				Rect timelineAreaRect = timelineAreaScope.rect;
				using( EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope( m_ScrollPos ) )
				{
					m_ScrollPos = scroll.scrollPosition;

					int index = 0;

					using( EditorGUILayout.VerticalScope timelineScope = new EditorGUILayout.VerticalScope() )
					{
						Rect timelineRect = timelineScope.rect;

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

											if( span.TotalDays >= 2.0 )
											{
												timestamp = status.createdAt.ToString( "d", s_CalcureInfo );
											}
											else if( span.TotalDays >= 1.0 )
											{
												timestamp = "昨日";
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

										EditorGUILayout.LabelField( status.text, EditorStyles.wordWrappedLabel );

										foreach( Attachment attachment in status.media_attachments )
										{
											DrawAttachment( attachment );
										}
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

						float scrollHeight = timelineRect.height - timelineAreaRect.height;

						if( scrollHeight > 0.0f )
						{
							float scrollRatio = m_ScrollPos.y / scrollHeight;

							if( scrollRatio >= 1.0f )
							{
								UpdateTimeline( false,true );
							}
						}
					}
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
