using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConsoleMusicPlayerSettings.NET
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			Closing += SettingsSave;

			if (File.Exists($"C:\\Users\\{Environment.UserName}" +
				"\\AppData\\Local\\ConsoleMusicPlayer\\Config.cfg"))
			{
				Properties.Resources.Culture =
					new System.Globalization.CultureInfo(
						File.ReadAllLines($"C:\\Users\\" +
						$"{Environment.UserName}\\AppData\\Local\\" +
						"ConsoleMusicPlayer\\Config.cfg")[0]
					);
			}
			else
			{
				Properties.Resources.Culture =
					new System.Globalization.CultureInfo("en");
			}

			InitializeComponent();
			Languages.SelectionChanged += LanguageChanged;

			if (File.Exists($"C:\\Users\\{Environment.UserName}" +
						"\\AppData\\Local\\ConsoleMusicPlayer\\Config.cfg"))
			{
				string[] data = File.ReadAllLines("C:\\Users\\" +
					$"{Environment.UserName}\\AppData\\Local\\" +
					"ConsoleMusicPlayer\\Config.cfg");

				Properties.Resources.Culture =
					new System.Globalization.CultureInfo(data[0]);

				Languages.Text = data[0] == "en" ? "English" : "Русский";
				VolumeSlider.Value = int.Parse(data[1]);
				GreetingCheckBox.IsChecked = bool.Parse(data[2]);
				ShuffleCheckBox.IsChecked = bool.Parse(data[3]);
			}
		}


		private void SettingsSave(object sender,
			System.ComponentModel.CancelEventArgs e)
		{
			MessageBoxResult saveResult = MessageBox.Show(
				Properties.Resources.Exit,
				"Exit",
				MessageBoxButton.YesNoCancel,
				MessageBoxImage.Question
			);

			switch (saveResult)
			{
				case MessageBoxResult.Yes:
					string[] saveData = new string[4] {
						Languages.Text == "English" ? "en" : "ru",
						VolumeSlider.Value.ToString(),
						GreetingCheckBox.IsChecked.ToString(),
						ShuffleCheckBox.IsChecked.ToString()
					};

					if (!Directory.Exists($"C:\\Users\\{Environment.UserName}" +
						"\\AppData\\ConsoleMusicPlayer"))
						Directory.CreateDirectory($"C:\\Users\\{Environment.UserName}" +
							"\\AppData\\Local\\ConsoleMusicPlayer");

					File.WriteAllLines($"C:\\Users\\{Environment.UserName}\\AppData\\" +
						"Local\\ConsoleMusicPlayer\\Config.cfg", saveData);

					MessageBox.Show(
						Properties.Resources.RestartToApply,
						"Restart the player",
						MessageBoxButton.OK,
						MessageBoxImage.Information
					);

					break;


				case MessageBoxResult.No:
					break;


				case MessageBoxResult.Cancel:
					e.Cancel = true;
					break;
			}
		}


		private void LanguageChanged(object sender, SelectionChangedEventArgs e)
		{
			switch (Languages.SelectedItem.ToString())
			{
				case "English":
					Properties.Resources.Culture = new System.Globalization.CultureInfo("en");
					break;
				case "Русский":
					Properties.Resources.Culture = new System.Globalization.CultureInfo("ru");
					break;
			}
		}
	}
}
