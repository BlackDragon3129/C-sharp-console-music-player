using System;

namespace ConsoleMusicPlayer.PlaylistNmSpc
{
	public static class PlaylistSaver
	{
		public static readonly string PLAYLIST_FILLING_PATH = $"C:\\Users\\{Environment.UserName}" +
			"\\AppData\\Local\\ConsoleMusicPlayer\\Playlists\\Filling\\";
		public static readonly string PLAYLIST_CONFIG_PATH = $"C:\\Users\\{Environment.UserName}" +
			"\\AppData\\Local\\ConsoleMusicPlayer\\Playlists\\Config\\";


		/// <summary>
		/// Save given playlist.
		/// Songs path: C:\Users\user_name\AppData\Local\ConsoleMusicPlayer\Playlists\Filling\
		/// Base path in C:\Users\user_name\AppData\Local\ConsoleMusicPlayer\Playlists\Config
		/// </summary>
		/// <param name="playlist">Playlist to save</param>
		/// <param name="fileName">Save file's name</param>
		public static void Save(Playlist playlist)
		{
			if (!Path.Exists(PLAYLIST_FILLING_PATH)) 
				Directory.CreateDirectory(PLAYLIST_FILLING_PATH);
			if (!Path.Exists(PLAYLIST_CONFIG_PATH))
				Directory.CreateDirectory(PLAYLIST_CONFIG_PATH);


			string fillingData = string.Empty;
			for (int i = 0; i < playlist.Songs.Count; i++)
				fillingData += playlist.Songs[i] + '\n';
			File.WriteAllText(PLAYLIST_FILLING_PATH + playlist.Name + ".plst", fillingData);

			File.WriteAllText(PLAYLIST_CONFIG_PATH + playlist.Name + ".cfg", playlist.BasePath);
		}


		/// <summary>
		/// Load playlist by file name
		/// </summary>
		/// <param name="playlistName">Playlist's to load name</param>
		/// <returns>Loaded playlist</returns>
		public static Playlist Load(string playlistName)
		{
			if (!File.Exists(PLAYLIST_FILLING_PATH + playlistName + ".plst") ||
				!File.Exists(PLAYLIST_CONFIG_PATH + playlistName + ".cfg"))
				throw new ArgumentException($"No saved playlist {playlistName} found!");

			string[] songs = File.ReadAllLines(PLAYLIST_FILLING_PATH + playlistName + ".plst");
			string basePath = File.ReadAllText(PLAYLIST_CONFIG_PATH + playlistName + ".cfg");
			return new Playlist(playlistName, basePath, songs);
		}


		/// <summary>
		/// Change playlist's save file's name
		/// </summary>
		/// <param name="oldPlaylistName">Old playlist's (and file's) name</param>
		/// <param name="newPlaylistName">New playlist's (and file's) name</param>
		public static void RenameSave(string oldPlaylistName, string newPlaylistName)
		{
			static void RenameFile(string oldFilePath, string newFilePath)
			{
				if (File.Exists(oldFilePath))
					File.Move(oldFilePath, newFilePath);
			}

			RenameFile(PLAYLIST_FILLING_PATH + oldPlaylistName + ".plst",
				PLAYLIST_CONFIG_PATH + newPlaylistName + ".plst");
			RenameFile(PLAYLIST_CONFIG_PATH + oldPlaylistName + ".cfg",
				PLAYLIST_CONFIG_PATH + newPlaylistName + ".cfg");
		}

		
		/// <summary>
		/// Delete playlist's save
		/// </summary>
		/// <param name="playlist">Deleting playlist</param>
		public static void DeleteSave(Playlist playlist)
		{
			try
			{
				File.Delete(PLAYLIST_FILLING_PATH + playlist.Name + ".plst");
				File.Delete(PLAYLIST_CONFIG_PATH + playlist.Name + ".cfg");
			}
			catch { }
		}
	}
}
