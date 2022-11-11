using FlashCap;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfApp10 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			this.ContentRendered += this.MainWindow_ContentRendered;
			this.link.Text = Directory.GetCurrentDirectory();
		}

		protected override void OnClosed( EventArgs e ) {
			base.OnClosed( e );

			this.captureDevice?.Dispose();
		}

		readonly CaptureDevices devices = new();
		CaptureDevice? captureDevice;

		CaptureDeviceDescriptor? SelectedDeviceDescriptor;
		VideoCharacteristics? SelectedVideoCharacteristics;


		private void MainWindow_ContentRendered( object? sender, EventArgs e ) {
			var items = this.devices.EnumerateDescriptors()
								.Where( x => x.Description != "VideoForWindows default" ) // 動画が取得できないため除去
								.ToArray();

			this.comboBox1.ItemsSource = items;
			this.comboBox1.SelectedItem = items.FirstOrDefault();
		}

		string? lastFile;

		private void button_Click( object sender, RoutedEventArgs e ) {

			var storyboard = new Storyboard() {
				Children = new TimelineCollection {
					new DoubleAnimation() { From = 1, To = 0, Duration = new Duration( TimeSpan.FromSeconds( 0.1 ) ) }
				},
			};
			Storyboard.SetTargetProperty( storyboard, new PropertyPath( Border.OpacityProperty ) );
			Storyboard.SetTarget( storyboard, border );
			storyboard.Begin();

			this.Dispatcher.Invoke( () => {
				if( this.image.Source is BitmapFrame bitmapFrame ) {
					lastFile = $"./{DateTime.Now.ToString( "yyyyMMdd_hhmmss" )}.jpg";

					using var fileStream = new FileStream( lastFile, FileMode.Create );
					var encoder = new JpegBitmapEncoder();
					encoder.Frames.Add( BitmapFrame.Create( bitmapFrame ) );
					encoder.Save( fileStream );
				}
			} );
		}

		private void comboBox1_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			this.SelectedDeviceDescriptor = (CaptureDeviceDescriptor)this.comboBox1.SelectedItem;

			var items = this.SelectedDeviceDescriptor.Characteristics;
			this.comboBox2.ItemsSource = items;
			this.comboBox2.SelectedItem = items.FirstOrDefault();
		}

		private async void comboBox2_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			this.SelectedVideoCharacteristics = (VideoCharacteristics)this.comboBox2.SelectedItem;

			await Start();
		}

		async Task Start() {
			if( this.SelectedDeviceDescriptor is null ) return;
			if( this.SelectedVideoCharacteristics is null ) return;

			this.captureDevice?.Stop();
			this.captureDevice?.Dispose();

			this.captureDevice = await this.SelectedDeviceDescriptor.OpenAsync(
				this.SelectedVideoCharacteristics,
				async bufferScope => {
					byte[] image = bufferScope.Buffer.ExtractImage();

					var ms = new MemoryStream( image );


					await Dispatcher.InvokeAsync( () => {
						var bitmap = BitmapFrame.Create( ms );
						bitmap.Freeze();
						this.image.Source = bitmap;
					} );

				} );

			this.captureDevice.Start();
		}

		private void Hyperlink_Click( object sender, RoutedEventArgs e ) {
			if( string.IsNullOrEmpty( lastFile ) ) {
				Process.Start( "EXPLORER.EXE", this.link.Text );
			} else {
				Process.Start( "EXPLORER.EXE", @$"/select, ""{Path.GetFullPath( lastFile )}""" );
			}
		}
	}
}
