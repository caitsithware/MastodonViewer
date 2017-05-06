using UnityEngine;
using UnityEditor;

namespace MastodonViewer
{
	static class Styles
	{
		public static GUIStyle entryBackEven;
		public static GUIStyle entryBackOdd;
		public static GUIStyle displayNameLabel;
		public static GUIStyle userNameLabel;
		public static GUIStyle timestampLabel;

		static Styles()
		{
			entryBackEven = new GUIStyle( (GUIStyle)"CN EntryBackEven" );
			entryBackEven.margin = new RectOffset( 0, 0, 0, 0 );
			entryBackEven.padding = new RectOffset( 5, 5, 5, 5 );
			entryBackEven.stretchWidth = true;

			entryBackOdd = new GUIStyle( (GUIStyle)"CN EntryBackOdd" );
			entryBackOdd.margin = new RectOffset( 0, 0, 0, 0 );
			entryBackOdd.padding = new RectOffset( 5, 5, 5, 5 );
			entryBackOdd.stretchWidth = true;

			displayNameLabel = EditorStyles.boldLabel;

			userNameLabel = new GUIStyle( EditorStyles.label );
			userNameLabel.margin = displayNameLabel.margin;
			userNameLabel.padding = displayNameLabel.padding;

			timestampLabel = userNameLabel;
		}
	}
}
