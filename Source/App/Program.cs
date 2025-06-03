using NAudio.Wave;
using System.Windows.Input;

using ConsoleMusicPlayer.PlaylistNmSpc;
using ConsoleMusicPlayer.MixerNmSpc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.X509Certificates;


namespace ConsoleMusicPlayer
{
	public static class Program
	{
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();


		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


		static async Task Main(string[] args)
		{

			#region Greeting
			Console.WriteLine($"{Localization.Hi}, {Environment.UserName}!\n");
			Console.WriteLine(Localization.VisitGitHub);

			#region LoadSettings
			int volume = 100;
			if (File.Exists($"C:\\Users\\{Environment.UserName}\\AppData\\" +
				"Local\\ConsoleMusicPlayer\\Config.cfg"))
			{
				string[] data = File.ReadAllLines("C:\\Users\\" +
					$"{Environment.UserName}\\AppData\\Local\\" +
					"ConsoleMusicPlayer\\Config.cfg");

				Localization.Culture = new System.Globalization.CultureInfo(data[0]);
				volume = int.Parse(data[1]);
				Config.Greeting = bool.Parse(data[2]);
				Config.ShuffleByDefault = bool.Parse(data[3]);

				if (Config.Greeting)
				{
					Console.WriteLine(Localization.SettingsLoad);
				}
			}
			else
			{
				if (!Directory.Exists($"C:\\Users\\{Environment.UserName}" +
					"\\AppData\\Local\\ConsoleMusicPlayer\\"))
				{
					Directory.CreateDirectory("C:\\Users\\" +
						$"{Environment.UserName}\\AppData\\Local\\" +
						"ConsoleMusicPlayer\\");
				}

				string[] saveData = [
					"en",
					"100",
					"True",
					"True"
				];
				File.WriteAllLines("C:\\Users\\" +
					$"{Environment.UserName}\\AppData\\Local\\" +
					"ConsoleMusicPlayer\\Config.cfg", saveData);
			}
			#endregion


			#region MainPlaylist
			if (Config.Greeting)
				Console.WriteLine(Localization.MainPlaylistLoad);

			Playlist mainPlaylist = new("Main", $"C:\\Users\\{Environment.UserName}" +
					"\\Music\\");
			try
			{
				mainPlaylist = PlaylistSaver.Load("Main");
			}
			catch (ArgumentException)
			{
				mainPlaylist = new("Main", $"C:\\Users\\{Environment.UserName}" +
					"\\Music\\");
				mainPlaylist.LoadFromBasePath();
				if (mainPlaylist.Songs.Count == 0)
				{
					File.Copy("Nothing.wav", mainPlaylist.BasePath);
					mainPlaylist.LoadFromBasePath();
				}
			}
			#endregion


			#region FeaturedPlaylist
			if (Config.Greeting)
				Console.WriteLine(Localization.FeaturedPlaylistLoad);
			Playlist featuredPlaylist = new("Featured", "C:\\");
			try
			{
				featuredPlaylist = PlaylistSaver.Load("Featured");
			}
			catch (ArgumentException) { }
			#endregion


			#region OtherPlaylists
			if (Config.Greeting)
				Console.WriteLine(Localization.PlaylistsLoad);

			List<Playlist> playlists = [mainPlaylist, featuredPlaylist];

			if (Path.Exists(PlaylistSaver.PLAYLIST_FILLING_PATH))
			{
				string[] playlistsFillings = Directory.GetFiles(PlaylistSaver.PLAYLIST_FILLING_PATH);
				for (int i = 0; i < playlistsFillings.Length; i++)
				{
					try
					{
						string fileName = playlistsFillings[i].Split('\\')[^1].Split('.')[0];
						if (fileName == mainPlaylist.Name || fileName == featuredPlaylist.Name)
							continue;

						playlists.Add(PlaylistSaver.Load(fileName));
					}
					catch (ArgumentException) { }
				}
			}
			#endregion


			#region Mixer
			if (Config.Greeting && !Config.ShuffleByDefault)
				Console.WriteLine(Localization.QueueCreate);
			else if (Config.Greeting && Config.ShuffleByDefault)
				Console.WriteLine(Localization.QueueCreateAndShuffle);

			Mixer mixer = new(mainPlaylist);
			mixer.SetVolume(volume);
			if (Config.ShuffleByDefault)
			{
				mixer.ShuffleQueue();
				mixer.LoadSongByIndex(0);
			}
			#endregion


			if (Config.Greeting)
				Console.WriteLine(Localization.EverythingReady + '\n');
			Console.WriteLine($"{mixer.GetSongName(mixer.CurrentSongIndex)} {Localization.PlayingNow}");
			#endregion


			bool TryGetPlaylistIndex(string playlistName, out int index)
			{
				for (int i = 0; i < playlists.Count; i++)
				{
					if (playlists[i].Name == playlistName)
					{
						index = i;
						return true;
					}
				}
				index = 0;
				return false;
			}

			void FeatureSong(string songPath)
			{
				if (featuredPlaylist.TryAdd(songPath))
					Console.WriteLine($"{songPath} {Localization.FeaturedSuccessfully}");
				else
					Console.WriteLine($"{songPath} {Localization.WasNotFeatured}");
			}


			var playTask = Task.Run(() => 
			{
				Thread.Sleep(Config.WaitingTime);

				while (true)
				{
					if (!mixer.IsPause)
					{
						if (mixer.Stopped)
						{
							try
							{
								mixer.LoadSongByIndex(mixer.CurrentSongIndex + 1);
							}
							catch (ArgumentException) { }
						}

						mixer.Play();
					}
					Thread.Sleep(Config.WaitingTime);
				}
			});

			var inputTask = Task.Run(() =>
			{
				while (true)
				{
					string command = Input(Localization.EnterCommand + ' ').Trim();

					if (command == "programm restart")
					{
						Console.Clear();

						string? exePath = Environment.ProcessPath;
						if (exePath != null)
							Process.Start(exePath);

						Environment.Exit(0);
					}	

					string[] commands = command.Split(' ');


					switch (commands[0].ToLower())
					{
						case "help":
							Process.Start("notepad.exe", "Help.txt");

							// Waiting, while the help is opening and then turn it into fullscreen
							while (true)
							{
								IntPtr handle = GetForegroundWindow();
								StringBuilder buffer = new(256);
								GetWindowText(handle, buffer, buffer.Capacity);

								string title = buffer.ToString();

								if (title.Contains("help.txt", StringComparison.OrdinalIgnoreCase))
								{
									ShowWindow(handle, 3);
									break;
								}

								Thread.Sleep(100);
							}
							break;

						case "settings":
							Process.Start("Settings\\" +
								"ConsoleMusicPlayerSettings.exe");
							break;

						case "github":
							Process.Start(new ProcessStartInfo
							{
								FileName = "https://github.com/BlackDragon3129",
								UseShellExecute = true
							});

							break;

						case "current":
							Console.WriteLine(mixer.GetSongName(mixer.CurrentSongIndex) + ' ' +
								Localization.PlayingNow);
							break;

						case "feature":
							try
							{
								string firstSong = string.Empty;
								string[] commandsToFirstSong = command.Split('|')[0].Split(' ');

								for (int i = 1; i < commandsToFirstSong.Length; i++)
									firstSong += commandsToFirstSong[i] + ' ';
								firstSong = firstSong.Trim();


								List<string> songs = [.. command.Split('|')[1..]];
								songs.Add(firstSong);


								for (int i = 0; i < songs.Count; i++)
								{
									if (int.TryParse(songs[i], out int songIndexInQueue))
									{
										songIndexInQueue--;
										try
										{
											FeatureSong(mixer.Queue[songIndexInQueue]);
										}
										catch (IndexOutOfRangeException)
										{
											Console.WriteLine($"{Localization.SongWithIndex} {songIndexInQueue} {
												Localization.SongNotFound}");
										}
									}
									else
									{
										FeatureSong(songs[i]);
									}
								}
							}
							catch (IndexOutOfRangeException) 
							{
								FeatureSong(mixer.Queue[mixer.CurrentSongIndex]);
							}

							PlaylistSaver.Save(featuredPlaylist);
							break;

						case "next":
							mixer.LoadSongByIndex(mixer.CurrentSongIndex + 1);
							Console.WriteLine(mixer.GetSongName(mixer.CurrentSongIndex) +
								Localization.PlayingNow);
							break;

						case "pause":
							mixer.SwitchPause();
							break;

						case "volume":
							try
							{
								if (int.TryParse(commands[1], out int volume))
								{
									mixer.SetVolume(volume);
									Console.WriteLine(Localization.MixerVolume + ' ' +
										mixer.Volume);

									if (File.Exists($"C:\\Users\\{Environment.UserName}\\AppData\\" +
										"Local\\ConsoleMusicPlayer\\Config.cfg"))
									{
										string[] saveData = File.ReadAllLines("C:\\Users\\" +
											$"{Environment.UserName}\\AppData\\Local\\" +
											"ConsoleMusicPlayer\\Config.cfg");
										saveData[1] = mixer.Volume.ToString();
										File.WriteAllLines("C:\\Users\\" +
											$"{Environment.UserName}\\AppData\\Local\\" +
											"ConsoleMusicPlayer\\Config.cfg", saveData);
									}
									else
									{
										if (!Directory.Exists($"C:\\Users\\{Environment.UserName}" +
											"\\AppData\\Local\\ConsoleMusicPlayer\\"))
										{
											Directory.CreateDirectory("C:\\Users\\" +
												$"{Environment.UserName}\\AppData\\Local\\" +
												"ConsoleMusicPlayer\\");
										}

										string[] saveData = [
											"en",
											mixer.Volume.ToString(),
											"True",
											"True"
										];
										File.WriteAllLines("C:\\Users\\" +
											$"{Environment.UserName}\\AppData\\Local\\" +
											"ConsoleMusicPlayer\\Config.cfg", saveData);
									}
								}
								else
									Console.WriteLine(Localization.WrongValueNeedNumber);
							}
							catch (IndexOutOfRangeException)
							{
								Console.WriteLine(Localization.MixerVolume + ' ' + mixer.Volume);
							}
							break;

						case "queue":
							for (int i = 0; i < mixer.Queue.Length; i++)
							{
								if (mixer.CurrentSongIndex == i)
									Console.Write('>');
								else
									Console.Write(' ');

								Console.WriteLine($" {i + 1}. {mixer.Queue[i]}");
							}
							break;

						case "shuffle":
							mixer.ShuffleQueue();
							Console.WriteLine(Localization.QueueShuffled);
							break;

						case "reload":
							mixer.LoadPlaylist(mixer.CurrentPlaylist);
							Console.WriteLine(Localization.Reloaded);
							break;

						case "reload_shuffle":
						case "shuffle_reload":
						case "reloadshuffle":
						case "shufflereload":
							mixer.LoadPlaylist(mixer.CurrentPlaylist);
							mixer.ShuffleQueue();
							if (mixer.Queue.Length > 0)
								mixer.LoadSongByIndex(0);
							Console.WriteLine(Localization.ReloadedAndShuffled);
							Console.WriteLine(mixer.GetSongName(mixer.CurrentSongIndex) + ' ' +
								Localization.PlayingNow);
							break;

						case "goto":
							if (int.TryParse(commands[1], out int index))
							{
								try
								{
									mixer.LoadSongByIndex(index - 1);
									Console.WriteLine(mixer.GetSongName(index - 1) + ' ' +
										Localization.PlayingNow);
								}
								catch (IndexOutOfRangeException)
								{
									Console.WriteLine(Localization.WrongIndex);
								}
							}
							else
							{
								if (commands[1].Equals("start", 
									StringComparison.CurrentCultureIgnoreCase))
								{
									mixer.LoadSongByIndex(0);
									Console.WriteLine(mixer.GetSongName(0) + ' ' +
										Localization.PlayingNow);
								}
								else if (commands[1].Equals("end",
									StringComparison.CurrentCultureIgnoreCase))
								{
									mixer.LoadSongByIndex(mixer.QueueLength - 1);
									Console.WriteLine(mixer.GetSongName(mixer.QueueLength - 1)
										+ ' ' + Localization.PlayingNow);
								}
								else
									Console.WriteLine(Localization.WrongValueNeedNumber);
							}

							break;

						case "playlist":
							try
							{
								switch (commands[1].ToLower())
								{
									case "current":
										Console.WriteLine(Localization.CurrentPlaylist + ' ' +
											mixer.CurrentPlaylist.Name);
										break;

									case "songs":
										for (int i = 0; i < mixer.CurrentPlaylist.Songs.Count; i++)
											Console.WriteLine($"{i + 1}. " +
												$"{mixer.CurrentPlaylist.Songs[i]}");
										break;

									case "rename":
										{
											if (mixer.CurrentPlaylist.Name == mainPlaylist.Name ||
												mixer.CurrentPlaylist.Name == featuredPlaylist.Name)
											{
												Console.WriteLine("You cannot rename main and featured playlists!");
												goto RenameCaseBreak;
											}

											string oldName = mixer.CurrentPlaylist.Name;
											string newName = Input(Localization.EnterNewPlaylistName + ' ');
											foreach (Playlist playlist in playlists)
											{
												if (playlist.Name == newName)
												{
													Console.WriteLine($"{Localization.Playlist} {newName} {Localization.PlaylistCurrentlyExists}");
													goto RenameCaseBreak;
												}
											}

											Console.WriteLine($"{Localization.Playlist} {mixer.CurrentPlaylist.Name} {Localization.PlaylistRenamed} {newName} {Localization.Successfully}");
											mixer.CurrentPlaylist.Name = newName;


											PlaylistSaver.RenameSave(oldName, newName);

										RenameCaseBreak:
											break;
										}

									case "dir":
									case "path":
									case "basepath":
									case "base_path":
										{
											try
											{
												if (mixer.CurrentPlaylist.Name == mainPlaylist.Name ||
													mixer.CurrentPlaylist.Name == featuredPlaylist.Name)
												{
													Console.WriteLine(Localization.CannotChangeBasePath);
												}

												string path = commands[2] + ' ';
												for (int i = 3; i < commands.Length; i++)
													path += commands[i] + ' ';
												path = path.Trim();

												if (mixer.CurrentPlaylist.
													TryChangeBasePath(path))
												{
													Console.WriteLine($"{Localization.PlaylistPathChanged} {path} {Localization.Successfully}!");
													PlaylistSaver.Save(mixer.CurrentPlaylist);
												}
												else
												{
													Console.WriteLine($"{path} {Localization.DoesNotExist}");
												}
											}
											catch (IndexOutOfRangeException)
											{
												Playlist currentPlaylist = mixer.CurrentPlaylist;
												Console.WriteLine($"{currentPlaylist.Name} {Localization.BasePath} {currentPlaylist.BasePath}");
											}

											break;
										}

									case "addsong":
									case "add_song":
										try
										{
											string firstSong = string.Empty;
											string[] commandsToFirstSong = command.Split('|')[0].Split(' ');

											for (int i = 2; i < commandsToFirstSong.Length; i++)
												firstSong += commandsToFirstSong[i] + ' ';
											firstSong = firstSong.Trim();


											List<string> songs = [.. command.Split('|')[1..]];
											songs.Add(firstSong);


											for (int i = 0; i < songs.Count; i++)
											{
												if (int.TryParse(songs[i], out int songIndexInPlaylist))
												{
													songIndexInPlaylist--;
													try
													{
														if (mixer.CurrentPlaylist.TryAdd(mixer.CurrentPlaylist.Songs[songIndexInPlaylist]))
															Console.WriteLine($"{mixer.CurrentPlaylist.Songs[songIndexInPlaylist]} {Localization.AddedToPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Successfully}!");
														else
															Console.WriteLine($"{mixer.CurrentPlaylist.Songs[songIndexInPlaylist]} {Localization.NotAddedToPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Not_ToPlaylistExplain}");
													}
													catch (IndexOutOfRangeException)
													{
														Console.WriteLine($"Song with index {songIndexInPlaylist} not found!");
													}
												}
												else
												{
													if (mixer.CurrentPlaylist.TryAdd(songs[i]))
														Console.WriteLine($"{songs[i]} {Localization.AddedToPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Successfully}!");
													else
														Console.WriteLine($"{songs[i]} {Localization.NotAddedToPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Not_ToPlaylistExplain}");
												}
											}
										}
										catch (IndexOutOfRangeException)
										{
											Console.WriteLine(Localization.WriteSongPath);
										}

										PlaylistSaver.Save(mixer.CurrentPlaylist);
										break;

									case "removesong": 
									case "remove_song":
										try
										{
											string firstSong = string.Empty;
											string[] commandsToFirstSong = command.Split('|')[0].Split(' ');

											for (int i = 2; i < commandsToFirstSong.Length; i++)
												firstSong += commandsToFirstSong[i] + ' ';
											firstSong = firstSong.Trim();


											List<string> songs = [.. command.Split('|')[1..]];
											songs.Add(firstSong);


											for (int i = 0; i < songs.Count; i++)
											{
												if (int.TryParse(songs[i], out int songIndexInPlaylist))
												{
													songIndexInPlaylist--;
													try
													{
														if (mixer.CurrentPlaylist.TryRemove(mixer.CurrentPlaylist.Songs[songIndexInPlaylist]))
															Console.WriteLine($"{mixer.CurrentPlaylist.Songs[songIndexInPlaylist]} {Localization.RemovedFromPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Successfully}!");
														else
															Console.WriteLine($"{mixer.CurrentPlaylist.Songs[songIndexInPlaylist]} {Localization.RemovedFromPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Not_ToPlaylistExplain}");
													}
													catch (IndexOutOfRangeException)
													{
														Console.WriteLine($"{Localization.SongWithIndex} {songIndexInPlaylist} {Localization.SongNotFound}");
													}
												}
												else
												{
													if (mixer.CurrentPlaylist.TryRemove(mixer.CurrentPlaylist.Songs[songIndexInPlaylist]))
														Console.WriteLine($"{mixer.CurrentPlaylist.Songs[i]} {Localization.RemovedFromPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Successfully}!");
													else
														Console.WriteLine($"{mixer.CurrentPlaylist.Songs[i]} {Localization.RemovedFromPlaylist} {mixer.CurrentPlaylist.Name} {Localization.Not_ToPlaylistExplain}");
												}
											}
										}
										catch (IndexOutOfRangeException)
										{
											Console.WriteLine(Localization.WriteSongPath);
										}

										PlaylistSaver.Save(mixer.CurrentPlaylist);
										break;

									case "featuresong": 
									case "feature_song":
										try
										{
											string firstSong = string.Empty;
											string[] commandsToFirstSong = command.Split('|')[0].Split(' ');

											for (int i = 2; i < commandsToFirstSong.Length; i++)
												firstSong += commandsToFirstSong[i] + ' ';
											firstSong = firstSong.Trim();


											List<string> songs = [.. command.Split('|')[1..]];
											songs.Add(firstSong);


											for (int i = 0; i < songs.Count; i++)
											{
												if (int.TryParse(songs[i], out int songIndexInPlaylist))
												{
													songIndexInPlaylist--;
													try
													{
														FeatureSong(mixer.CurrentPlaylist.Songs[songIndexInPlaylist]);
													}
													catch (IndexOutOfRangeException)
													{
														Console.WriteLine($"Song with index {songIndexInPlaylist} not found!");
													}
												}
												else
												{
													FeatureSong(songs[i]);
												}
											}
										}
										catch (IndexOutOfRangeException)
										{
											FeatureSong(mixer.Queue[mixer.CurrentSongIndex]);
										}

										PlaylistSaver.Save(featuredPlaylist);
										break;

									case "replacesong": 
									case "replace_song":
										{
											if (commands[2] == commands[3])
											{
												Console.WriteLine("Songs must be different!");
												goto ReplaceSongCaseBreak;
											}

											bool song1Found = int.TryParse(commands[2], out int song1Index),
												song2Found = int.TryParse(commands[3], out int song2Index);

											if (!song1Found || !song2Found)
											{
												Console.WriteLine(Localization.OneSongNotFound);
											}
											else
											{
												song1Index--; song2Index--;

												(mixer.CurrentPlaylist.Songs[song1Index],
													mixer.CurrentPlaylist.Songs[song2Index]) =
													(mixer.CurrentPlaylist.Songs[song2Index],
													mixer.CurrentPlaylist.Songs[song1Index]);
												Console.WriteLine($"{mixer.CurrentPlaylist.Songs[song1Index]} {Localization.And} {mixer.CurrentPlaylist.Songs[song2Index]} {Localization.ReplacedSuccessfully}");
												PlaylistSaver.Save(mixer.CurrentPlaylist);
											}

										ReplaceSongCaseBreak:
											break;
										}

									case "create":
										string playlistName = Input(Localization.EnterPlaylistName + ' ');
										for (int i = 0; i < playlists.Count; i++)
										{
											if (playlists[i].Name == playlistName)
											{
												Console.WriteLine($"Playlist {playlistName} currently exists!");
												goto CreatePlaylistCaseBreak;
											}
										}

										string basePath = Input(Localization.EnterBasePath + ' ');
										if (!Path.Exists(basePath))
										{
											Console.WriteLine($"Path {basePath} does not exists!");
											goto CreatePlaylistCaseBreak;
										}

										Playlist newPlaylist = new(playlistName, basePath);

										if (IsUserSure(Localization.LoadSongsFromBasePath + ' '))
										{
											newPlaylist.LoadFromBasePath();
											Console.WriteLine($"{Localization.SongsFrom} {basePath} {Localization.SongsLoadedSuccessfully}");
										}

										Console.WriteLine($"{Localization.Playlist} {playlistName} {Localization.PlaylistCreated}");
										playlists.Add(newPlaylist);
										PlaylistSaver.Save(newPlaylist);

									CreatePlaylistCaseBreak:
										break;

									case "delete":
										{
											if (!int.TryParse(commands[2], out int playlistIndex))
											{
												for (int i = 0; i < playlists.Count; i++)
												{
													if (playlists[i].Name == commands[2])
													{
														playlistIndex = i + 1;
														break;
													}
												}
											}

											try
											{
												playlistIndex--;
												if (playlistIndex < 0)
												{
													Console.WriteLine(Localization.WrongPlaylistName);
												}
												else if (playlists[playlistIndex].Name == mainPlaylist.Name ||
													playlists[playlistIndex].Name == featuredPlaylist.Name)
												{
													Console.WriteLine(Localization.MainOrFeatureDelete);
												}
												else
												{
													if (IsUserSure($"{Localization.SureToDeletePlaylist} {playlists[playlistIndex].Name}? {Localization.YesNo} "))
													{
														Console.WriteLine($"{playlists[playlistIndex].Name} {Localization.PlaylistDeleted}");
														PlaylistSaver.DeleteSave(playlists[playlistIndex]);
														playlists.RemoveAt(playlistIndex);
													}
												}
											}
											catch (IndexOutOfRangeException)
											{
												Console.WriteLine(Localization.WrongPlaylistName);
											}

											break;
										}

									case "get":
										for (int i = 0; i < playlists.Count; i++)
											Console.WriteLine($"{i + 1}. {playlists[i].Name}");
										break;

									case "set":
										{
											if (!int.TryParse(commands[2], out int playlistIndex))
											{
												if (TryGetPlaylistIndex(commands[2], out playlistIndex))
												{
													playlistIndex++;
												}
												else
												{
													Console.WriteLine($"{Localization.Playlist} {commands[2]} {Localization.DoesNotExist}");
													goto SetPlaylistBreak;
												}
											}

											try
											{
												playlistIndex--;
												if (playlistIndex < 0)
												{
													Console.WriteLine(Localization.WrongPlaylistName);
												}
												else
												{
													mixer.LoadPlaylist(playlists[playlistIndex]);
													if (playlists[playlistIndex].Songs.Count == 0)
														goto SetPlaylistBreak;

													if (Config.ShuffleByDefault)
													{
														mixer.ShuffleQueue();
														mixer.LoadSongByIndex(0);
													}

													Console.WriteLine($"{Localization.Playlist} {playlists[playlistIndex].Name} {Localization.PlaylistLoadedSuccessfully}");
													Console.WriteLine($"{mixer.GetSongName(mixer.CurrentSongIndex)} {Localization.PlayingNow}");
												}
											}
											catch (IndexOutOfRangeException)
											{
												Console.WriteLine(Localization.WrongPlaylistName);
											}
										SetPlaylistBreak:
											break;
										}

									default:
										Console.WriteLine(Localization.WrongCommand);
										break;
								}
							}
							catch (IndexOutOfRangeException)
							{
								Console.WriteLine(Localization.WrongCommand);
							}
							break;

						default:
							Console.WriteLine(Localization.WrongCommand);
							break;
					}

					Console.WriteLine();
				}
			});

			await Task.WhenAll(playTask, inputTask);
		}

		
		static string Input(string prompt)
		{
			Console.Write(prompt);
			string? userInput = Console.ReadLine();

			return userInput == null ? throw new FormatException("Input cannot be null!") 
				: userInput;
		}

		static bool IsUserSure(string prompt)
		{
			string isTheySure = Input(prompt).ToLower();
			if (isTheySure == "yes" || isTheySure == "y")
			{
				return true;
			}
			else if (isTheySure != "yes" && isTheySure != "y")
			{
				if (isTheySure != "no" && isTheySure != "n")
					Console.WriteLine("That means \"no\"");
				return false;
			}
			return false;
		}
	}
}
