using System;
using System.Collections;
using System.Xml.Linq;
using System.IO;

namespace ConsoleMusicPlayer.PlaylistNmSpc
{
	/// <summary>
	/// Playlist class contains songs. User can add, replace and remove songs from the
	/// playlist
	/// </summary>
	public class Playlist
	{
		private readonly List<string> m_songs;
		/// <summary>
		/// List of the playlist's song. Changes by "TryAdd", "Replace" "TryRemove"
		/// </summary>
		public List<string> Songs { get => m_songs; }


		/// <summary>
		/// Playlist's name
		/// </summary>
		public string Name;


		private string m_basePath;
		/// <summary>
		/// Base directory of the playlist. It is used for remove, add and replace 
		/// songs by their files names and not the full path
		/// </summary>
		public string BasePath { get => m_basePath; }


		public static readonly string[] MusicFilesExtensions = [".mp3", ".ogg", ".wav"];


		public Playlist(string name, string basePath)
		{
			Name = name.Trim();
			Name ??= "NewPlaylist";
			m_basePath = basePath.EndsWith('\\')? basePath : basePath + '\\';

			m_songs = [];
		}
		public Playlist(string name, string basePath, string[] songs)
		{
			m_songs = [];
			for (int i = 0; i < songs.Length; i++)
				if (File.Exists(songs[i]))
					m_songs.Add(songs[i]);

			Name = name.Trim();
			Name ??= "NewPlaylist";

			m_basePath = basePath.EndsWith('\\') ? basePath : basePath + '\\';
		}
		public Playlist(string name, string basePath, List<string> songs)
		{
			m_songs = songs;
			Name = name.Trim();
			Name ??= "NewPlaylist";

			m_basePath = basePath.EndsWith('\\') ? basePath : basePath + '\\';
		}


		/// <summary>
		/// Add a song to the playlist if it doesn't exist here right now
		/// </summary>
		/// <param name="newSongPath"></param>
		/// <returns>True if new song doesn't exist there, false if the song if
		/// in the playlist</returns>
		public bool TryAdd(string newSongPath)
		{
			if (Path.GetDirectoryName(newSongPath) == string.Empty)
				newSongPath = m_basePath + newSongPath;

			if (!File.Exists(newSongPath))
				return false;

			if (!MusicFilesExtensions.Contains(Path.GetExtension(newSongPath)))
				return false;

			foreach (string song in m_songs)
				if (song == newSongPath)
					return false;

			m_songs.Add(newSongPath);
			return true;
		}


		/// <summary>
		/// Change song's index by it's base index in the playlist
		/// </summary>
		/// <param name="oldIndex">Song's base index in the playlist</param>
		/// <param name="newIndex">New song's index in the playlist</param>
		public void Replace(int oldIndex, int newIndex)
		{
			string acc = m_songs[oldIndex];

			m_songs.RemoveAt(oldIndex);
			m_songs.Insert(newIndex, acc);
		}


		/// <summary>
		/// Change song's index by it's name
		/// </summary>
		/// <param name="song">Song's name</param>
		/// <param name="newIndex">New song's index in the playlist</param>
		/// <exception cref="ArgumentException">Thrown if the song not found</exception>
		public void Replace(string song, int newIndex)
		{
			if (Path.GetDirectoryName(song) == string.Empty)
				song = m_basePath + song;

			int songIndex = -1;
			for (int i = 0; i < m_songs.Count; i++)
			{
				if (m_songs[i] == song)
				{
					songIndex = i;
					break;
				}
			}

			if (songIndex == -1)
				throw new ArgumentException($"\"{song}\" not found in the playlist! " +
					$"Maybe you need to use \"add\"?");

			m_songs.RemoveAt(songIndex);
			m_songs.Insert(newIndex, song);
		}


		/// <summary>
		/// Remove song from the playlist by song's name
		/// </summary>
		/// <param name="song">Song's name</param>
		/// <returns>True if the song was removed succesful, false if not</returns>
		public bool TryRemove(string song)
		{
			if (Path.GetDirectoryName(song) == string.Empty)
				song = m_basePath + song;

			int songIndex = -1;
			for (int i = 0; i < m_songs.Count; i++)
			{
				if (m_songs[i] == song)
				{
					songIndex = i;
					break;
				}
			}

			if (songIndex == -1)
				return false;

			m_songs.RemoveAt(songIndex);
			return true;
		}


		/// <summary>
		/// Remove song from the playlist by song's index
		/// </summary>
		/// <param name="index">Song's index</param>
		/// <returns>True if the song was removed succesful, false if not</returns>
		public bool TryRemove(int index)
		{
			try
			{
				m_songs.RemoveAt(index);
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
		}


		/// <summary>
		/// Load every song with .mp3, .ogg and .wav extensins from the base path
		/// </summary>
		public void LoadFromBasePath()
		{
			string[] musicFiles = Directory.GetFiles(m_basePath).
				Where(f => MusicFilesExtensions.Contains(
					Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).ToArray();


			foreach (string filePath in musicFiles)
				TryAdd(filePath);
		}

		
		/// <summary>
		/// Change playlist's base path
		/// </summary>
		/// <param name="newPath"></param>
		/// <returns>True if new base path exists, otherwise false</returns>
		public bool TryChangeBasePath(string newPath)
		{
			if (!newPath.EndsWith('\\'))
				newPath += '\\';

			if (Path.Exists(newPath))
			{
				m_basePath = newPath;
				return true;
			}
			return false;
		}


		/// <summary>
		/// Get song's index
		/// </summary>
		/// <param name="songName">Song's full path or song's name with it's extension
		/// if it is in the playlist's base path</param>
		/// <param name="index">Song's index</param>
		/// <returns>True if song was found, otherwise false. Out song's index</returns>
		public bool TryGetSongIndex(string songName, out int index)
		{
			index = m_songs.IndexOf(songName);
			if (index == -1)
			{
				index = m_songs.IndexOf(m_basePath + songName);
				if (index == -1)
				{
					index = 0;
					return false;
				}
			}
			return true;
		}
	}
}
