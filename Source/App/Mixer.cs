using System;
using System.Reflection.Metadata;
using ConsoleMusicPlayer.PlaylistNmSpc;
using NAudio.Mixer;
using NAudio.Wave;

namespace ConsoleMusicPlayer.MixerNmSpc
{
	public class Mixer
	{
		private string[] m_queue;
		/// <summary>
		/// Sorted list of playlist's songs
		/// </summary>
		public string[] Queue { get => m_queue; }
		/// <summary>
		/// Length of the queue
		/// </summary>
		public int QueueLength { get => m_queue.Length; }


		private int m_currentSongIndex = 0;
		/// <summary>
		/// Current song's index in the queue
		/// </summary>
		public int CurrentSongIndex { get => m_currentSongIndex; }


		private Playlist m_currentPlaylist;
		/// <summary>
		/// Playlist which is being playing
		/// </summary>
		public Playlist CurrentPlaylist { get => m_currentPlaylist; }


		private readonly WaveOutEvent m_outputDevice;
		/// <summary>
		/// Get mixer's volume
		/// </summary>
		public int Volume { get => (int)Math.Round(m_outputDevice.Volume * 100, 0); }


		private bool m_isPause;
		public bool IsPause { get => m_isPause; }


		public bool Stopped { get => m_outputDevice.PlaybackState == PlaybackState.Stopped; }


		/// <summary>
		/// Load playlist and get output device
		/// </summary>
		/// <param name="playlist">Main playlist</param>
		public Mixer(Playlist playlist)
		{
			m_outputDevice = new()
			{
				Volume = 1.0f
			};
			m_isPause = false;

			LoadPlaylist(playlist);
		}


		/// <summary>
		/// Insert playlist's songs to the queue and shuffle the queue
		/// </summary>
		/// <param name="playlist">Playlist to load</param>
		public void LoadPlaylist(Playlist playlist)
		{
			m_outputDevice.Stop();

			m_queue = [.. playlist.Songs];
			m_currentPlaylist = playlist;
			m_currentSongIndex = 0;


			try
			{
				if (m_currentPlaylist.Songs.Count > 0)
					LoadSongByIndex(0);
				else
					Console.WriteLine($"{m_currentPlaylist.Name} is empty playlist :( You should add songs to this playlist or delete it");
			}
			catch (ArgumentException) { }
		}


		/// <summary>
		/// If output device is not null, load and play the song. If the song has ended, play next 
		/// song in the queue If the queue has ended, reload the playlist. 
		/// </summary>
		public void Play()
		{
			if (m_outputDevice != null && m_outputDevice.PlaybackState == PlaybackState.Stopped)
			{
				try
				{
					AudioFileReader audioFile = new(m_queue[m_currentSongIndex]);
					m_outputDevice.Init(audioFile);

					m_outputDevice.Play();
				}
				catch (IndexOutOfRangeException) { }
			}
		}


		/// <summary>
		/// Pause and unpause
		/// </summary>
		public void SwitchPause()
		{
			m_isPause = !m_isPause;

			if (m_outputDevice != null)
			{
				if (m_isPause)
					m_outputDevice.Pause();
				else
				{
					try
					{
						m_outputDevice.Play();
					}
					catch { }
				}
			}
		}


		/// <summary>
		/// Play song in the queue by its index there
		/// </summary>
		public void LoadSongByIndex(int newSongIndex)
		{
			m_outputDevice.Stop();

			if (newSongIndex < 0 || newSongIndex > m_queue.Length)
				throw new ArgumentException($"There isn't song with index {newSongIndex}" +
					$" in the queue!");

			m_currentSongIndex = newSongIndex;
			if (m_currentSongIndex >= m_queue.Length)
			{
				LoadPlaylist(m_currentPlaylist);
				if (Config.ShuffleByDefault)
				{
					ShuffleQueue();
					LoadSongByIndex(0);
				}
			}

			Play();
		}


		/// <summary>
		/// Get song's name with its extension without its full path by its index
		/// in the queue
		/// </summary>
		public string GetSongName(int index)
		{
			string[] pathParts = m_queue[index].Split('\\');
			return pathParts[^1];
		}


		/// <summary>
		/// Set volume to the mixer
		/// </summary>
		/// <param name="volume">Mixer's new volume</param>
		public void SetVolume(int volume)
		{
			volume = Math.Clamp(volume, 0, 100);
			m_outputDevice.Volume = (float)Math.Round(volume / 100f, 2);
		}


		/// <summary>
		/// Shuffle playlist's songs list
		/// </summary>
		/// <param name="array">Playlist's songs list</param>
		/// <returns>Shuffled array</returns>
		public void ShuffleQueue()
		{
			Random random = new();
			m_queue = [.. m_currentPlaylist.Songs];

			try
			{
				string currentSong = m_queue[m_currentSongIndex];

				for (int i = m_queue.Length - 1; i >= 0; i--)
				{
					int newElementPosition = random.Next(i + 1);

					(m_queue[i], m_queue[newElementPosition]) =
						(m_queue[newElementPosition], m_queue[i]);
				}

				m_currentSongIndex = Array.IndexOf(m_queue, currentSong);
			}
			catch (IndexOutOfRangeException) { }
		}
	}
}
