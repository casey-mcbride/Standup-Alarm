using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using StandupAlarm.Models.StandupMessengers;

namespace StandupAlarm.Activities
{
	public class StopAlarmActivity : Activity, TextToSpeech.IOnInitListener
	{
		#region Constants

		private static readonly long[] VIBRATION_PATTERN = new long[] { 0, 500, 500, 500, 500, 500};

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		private IStandupMessenger currentMessenger;

		#endregion

		#region Properties

		public Button ButtonStopAlarm
		{
			get { return this.FindViewById<Button>(Resource.Id.buttonStopAlarm); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			// Make this page show evenwhen the phone is locked
			Window.AddFlags(WindowManagerFlags.ShowWhenLocked);
			Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			Window.AddFlags(WindowManagerFlags.TurnScreenOn);

			Vibrator vb = this.GetSystemService(Java.Lang.Class.FromType(typeof(Vibrator))) as Vibrator;
			if (vb != null && vb.HasVibrator)
				vb.Vibrate(VIBRATION_PATTERN, -1);

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.StopAlarmView);

			ButtonStopAlarm.Click += ButtonStopAlarm_Click;

			this.speechEngine = new TextToSpeech(this, this);
		}

		#endregion

		#region Methods

		protected override void OnStop()
		{
			this.speechEngine.Stop();
			base.OnStop();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}

		#endregion

		#region Event Handlers

		private void ButtonStopAlarm_Click(object sender, EventArgs e)
		{
			if (currentMessenger != null)
				currentMessenger.Stop();
			this.Finish();
		}

		public void OnInit(OperationResult status)
		{
			// Occurs when the speech engine is initialized
			System.Diagnostics.Debug.Assert(currentMessenger == null);

			// TODO: Don't play this when intialized, play this at the right time
			currentMessenger = MessengerFactory.CreateMessenger(speechEngine, DateTime.Now);
			currentMessenger.Start();
		}

		#endregion
	}
}