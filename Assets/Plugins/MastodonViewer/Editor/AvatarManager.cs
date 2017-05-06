using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MastodonViewer
{
	public class AvatarManager : ScriptableSingleton<AvatarManager> , ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<Avatar> m_Avatars = new List<Avatar>();

		private Dictionary<string, Avatar> m_DicAvatars = new Dictionary<string, Avatar>();

		public Texture2D GetAvatarTexture( string avatarUrl )
		{
			Avatar avatar = null;

			if( !avatarUrl.StartsWith( "https:" ) )
			{
				avatarUrl = "https://unityjp-mastodon.tokyo" + avatarUrl;
			}

			if( m_DicAvatars.TryGetValue( avatarUrl, out avatar ) )
			{
				if( avatar.isDone )
				{
					return avatar.avatarTexture;
				}
				return null;
			}

			avatar = new Avatar( avatarUrl );

			m_Avatars.Add( avatar );
			m_DicAvatars.Add( avatarUrl, avatar );

			return null;
		}

		public void OnAfterDeserialize()
		{
			m_DicAvatars.Clear();
			foreach( Avatar avatar in m_Avatars )
			{
				m_DicAvatars.Add( avatar.url, avatar );
			}
		}

		public void OnBeforeSerialize()
		{
		}

		public bool UpdateAvatar()
		{
			bool updated = false;
			foreach( KeyValuePair<string, Avatar> pair in m_DicAvatars )
			{
				Avatar avatar = pair.Value;

				if( avatar.Update() )
				{
					updated = true;
				}
			}

			return updated;
		}
	}
}
