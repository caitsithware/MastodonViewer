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

		private static Dictionary<string, Avatar> _Avatars = new Dictionary<string, Avatar>();

		static class Styles
		{
			public static GUIStyle entryBackEven;
			public static GUIStyle entryBackOdd;

			static Styles()
			{
				entryBackEven = new GUIStyle((GUIStyle)"CN EntryBackEven");
				entryBackEven.margin = new RectOffset(0,0,0,0);
				entryBackOdd = new GUIStyle((GUIStyle)"CN EntryBackOdd");
				entryBackOdd.margin = new RectOffset( 0, 0, 0, 0 );
			}
		}

		public class Avatar
		{
			private WWW m_WWWAvatarTexture;
			private Texture2D m_AvatarTexture;

			private bool m_IsDone = false;
			public bool isDone
			{
				get
				{
					return m_IsDone;
				}
			}

			public Texture2D avatarTexture
			{
				get
				{
					return m_AvatarTexture;
				}
			}

			public Avatar( string url )
			{
				m_WWWAvatarTexture = new WWW( url );
			}

			public bool Update()
			{
				bool updated = false;

				if( m_WWWAvatarTexture != null && m_WWWAvatarTexture.isDone )
				{
					m_AvatarTexture = m_WWWAvatarTexture.texture as Texture2D;

					m_WWWAvatarTexture.Dispose();

					m_WWWAvatarTexture = null;

					updated = true;
					m_IsDone = true;
				}

				return updated;
			}
		}

		[MenuItem( "Window/Mastodon Viewer" )]
		static void Open()
		{
			EditorWindow.GetWindow<MastodonViewerWindow>();
		}

		public static Texture2D GetAvatarTexture( string avatarUrl )
		{
			Avatar avatar = null;

			if( !avatarUrl.StartsWith("https:") )
			{
				avatarUrl = "https://unityjp-mastodon.tokyo" + avatarUrl;
			}

			if( _Avatars.TryGetValue( avatarUrl, out avatar ) )
			{
				if( avatar.isDone )
				{
					return avatar.avatarTexture;
				}
				return null;
			}

			avatar = new Avatar( avatarUrl );

			_Avatars.Add( avatarUrl, avatar );

			return null;
		}

		public static bool UpdateAvatar()
		{
			bool updated = false;
			foreach( KeyValuePair<string, Avatar> pair in _Avatars )
			{
				Avatar avatar = pair.Value;

				if( avatar.Update() )
				{
					updated = true;
				}
			}

			return updated;
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
					return GetAvatarTexture( avatar );
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

			private bool m_InitializedCreatedAt = false;
			private DateTime m_CreatedAt;
			public TimeSpan m_Span;

			void InitializeDate()
			{
				if( !m_InitializedCreatedAt )
				{
					m_CreatedAt = DateTime.Parse( created_at );

					m_Span = DateTime.Now - m_CreatedAt;
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
					InitializeDate();

					return m_Span;
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

		private void OnInspectorUpdate()
		{
			if( m_WWWTimelines != null && m_WWWTimelines.isDone )
			{
				m_TimeLine = JsonUtility.FromJson<Timeline>( "{ \"statuses\": " + m_WWWTimelines.text + " }" );

				m_WWWTimelines.Dispose();

				m_WWWTimelines = null;
			}

			if( UpdateAvatar() )
			{
				Repaint();
			}
		}

		private void OnGUI()
		{
			if( m_WWWTimelines != null )
			{
				EditorGUILayout.LabelField( "読み込み中" );
			}

			if( m_WWWTimelines == null )
			{
				if( GUILayout.Button( "更新" ) )
				{
					m_WWWTimelines = new WWW( s_URL );
				}
			}

			if( m_TimeLine != null )
			{
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
										EditorGUILayout.LabelField( status.account.display_name, EditorStyles.boldLabel );
										EditorGUILayout.LabelField( "@"+status.account.username );

										GUILayout.FlexibleSpace();

										string time = string.Empty;
										if( status.span.TotalDays >= 1.0 )
										{
											time = status.createdAt.ToString( "d" );
										}
										else if( status.span.Hours >= 1 )
										{
											time = status.span.Hours + "時間前";
										}
										else if( status.span.Minutes >= 1 )
										{
											time = status.span.Minutes + "分前";
										}
										else
										{
											time = status.span.Seconds + "秒前";
										}

										EditorGUILayout.LabelField( time );
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
		}
	}
}
