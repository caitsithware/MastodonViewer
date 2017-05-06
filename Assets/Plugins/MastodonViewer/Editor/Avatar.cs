using System;
using UnityEngine;

namespace MastodonViewer
{
	[Serializable]
	public class Avatar
	{
		private string m_Url;
		private WWW m_WWWAvatarTexture;
		private Texture2D m_AvatarTexture;

		public string url
		{
			get
			{
				return m_Url;
			}
		}

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
			m_Url = url;
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
}
