using System;
using UnityEngine;

namespace MastodonViewer
{
	[Serializable]
	public class Media
	{
		private string m_Url;
		private WWW m_WWWMediaTexture;
		private Texture2D m_MediaTexture;

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

		public Texture2D mediaTexture
		{
			get
			{
				return m_MediaTexture;
			}
		}

		public Media( string url )
		{
			m_Url = url;
			m_WWWMediaTexture = new WWW( url );
		}

		public bool Update()
		{
			bool updated = false;

			if( m_WWWMediaTexture != null && m_WWWMediaTexture.isDone )
			{
				m_MediaTexture = m_WWWMediaTexture.texture as Texture2D;

				m_WWWMediaTexture.Dispose();

				m_WWWMediaTexture = null;

				updated = true;
				m_IsDone = true;
			}

			return updated;
		}
	}
}
