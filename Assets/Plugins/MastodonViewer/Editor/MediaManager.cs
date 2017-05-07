using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MastodonViewer
{
	public class MediaManager : ScriptableSingleton<MediaManager> , ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<Media> m_Medias = new List<Media>();

		private Dictionary<string, Media> m_DicMedias = new Dictionary<string, Media>();

		public Texture2D GetMediaTexture( string mediaUrl )
		{
			Media avatar = null;

			if( !mediaUrl.StartsWith( "https:" ) )
			{
				mediaUrl = @"https://unityjp-mastodon.tokyo" + mediaUrl;
			}

			if( m_DicMedias.TryGetValue( mediaUrl, out avatar ) )
			{
				if( avatar.isDone )
				{
					return avatar.mediaTexture;
				}
				return null;
			}

			avatar = new Media( mediaUrl );

			m_Medias.Add( avatar );
			m_DicMedias.Add( mediaUrl, avatar );

			return null;
		}

		public void OnAfterDeserialize()
		{
			m_DicMedias.Clear();
			foreach( Media avatar in m_Medias )
			{
				m_DicMedias.Add( avatar.url, avatar );
			}
		}

		public void OnBeforeSerialize()
		{
		}

		public bool UpdateMedia()
		{
			bool updated = false;
			foreach( KeyValuePair<string, Media> pair in m_DicMedias )
			{
				Media avatar = pair.Value;

				if( avatar.Update() )
				{
					updated = true;
				}
			}

			return updated;
		}
	}
}
