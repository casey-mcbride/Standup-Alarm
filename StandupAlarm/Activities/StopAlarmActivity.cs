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
using Android.Telephony;
using Android.Views;
using Android.Widget;
using StandupAlarm.Models;
using StandupAlarm.Models.StandupMessengers;
using StandupAlarm.Persistance;

namespace StandupAlarm.Activities
{
	public class StopAlarmActivity : Activity, TextToSpeech.IOnInitListener
	{
		#region Constants

		private static readonly long[] VIBRATION_PATTERN = new long[] { 0, 500, 500, 500, 500, 500};

		private const string TIME_FORMAT_STRING = "s\\.fff";

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		private IStandupMessenger currentMessenger;

		private CountDownTimer timer;

		private bool timeDone = false;

		private bool voiceReady = false;

		private bool wasStopped = false;

		#endregion

		#region Properties

		public Button ButtonStopAlarm
		{
			get { return this.FindViewById<Button>(Resource.Id.buttonStopAlarm); }
		}

		public TextView TextStartTimeDisplay
		{
			get { return this.FindViewById<TextView>(Resource.Id.textStopAlarmTimeToGoOff); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Make this page show evenw hen the phone is locked
			Window.AddFlags(WindowManagerFlags.ShowWhenLocked);
			//Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			Window.AddFlags(WindowManagerFlags.TurnScreenOn);

			Settings.AddLogMessage(this, "Stop screen started: {0}", DateTime.Now.ToString(ApplicationState.DATE_TIME_TIME_OF_DAY_FORMAT_STRING));

			// If we have location constraints, make sure we're near them
			HashSet<int> validIDs = Settings.GetValidCellTowerIDs(this);
			if(validIDs.Any())
			{
				HashSet<int> cellTowerIDsNearby = ApplicationState.GetInstance(this).GetNearbyCellTowerIDs();

				if(!validIDs.Overlaps(cellTowerIDsNearby))
				{
					// Cancel this activity
					stopAlarm();
					Finish();
					Settings.AddLogMessage(this, "Not near any valid cell towers. {0}", string.Join(", ", cellTowerIDsNearby));
					return;
				}
			}

			Vibrator vb = this.GetSystemService(Java.Lang.Class.FromType(typeof(Vibrator))) as Vibrator;

#pragma warning disable 618
			if (vb != null && vb.HasVibrator)
				vb.Vibrate(VIBRATION_PATTERN, -1);
#pragma warning restore 618

			SetContentView(Resource.Layout.StopAlarmView);

			ButtonStopAlarm.Click += ButtonStopAlarm_Click;

			// Set the alarm volume to a constant loud volume
			VolumeControlStream = Stream.Alarm;
			AudioManager manager = (AudioManager)GetSystemService(Context.AudioService);
			manager.SetStreamVolume(Stream.Alarm, manager.GetStreamMaxVolume(Stream.Alarm), VolumeNotificationFlags.AllowRingerModes);

			this.speechEngine = new TextToSpeech(this, this);

			TextStartTimeDisplay.Text = ApplicationState.SHUT_OFF_WARNING_TIME.TotalSeconds.ToString(TIME_FORMAT_STRING);

			timer = new StartAlarmTimer((long)ApplicationState.SHUT_OFF_WARNING_TIME.TotalMilliseconds, (long)TimeSpan.FromMilliseconds(7).TotalMilliseconds, this);
			timer.Start();
		}

		private class StartAlarmTimer : CountDownTimer
		{
			StopAlarmActivity owner;

			public StartAlarmTimer(long startMS, long countdownIntervalMS, StopAlarmActivity owner)
				:base (startMS, countdownIntervalMS)
			{
				this.owner = owner;
			}

			public override void OnTick(long millisUntilFinished)
			{
				owner.TextStartTimeDisplay.Text = TimeSpan.FromMilliseconds(millisUntilFinished).ToString(TIME_FORMAT_STRING);
			}

			public override void OnFinish()
			{
				Settings.AddLogMessage(owner, "Time delay finished at {0}", DateTime.Now.ToString(ApplicationState.DATE_TIME_TIME_OF_DAY_FORMAT_STRING));
				owner.TextStartTimeDisplay.Text = "0";
				owner.timeDone = true;
				owner.speak();
			}
		}

		#endregion

		#region Methods

		protected override void OnStop()
		{
			if(this.speechEngine != null)
				this.speechEngine.Stop();
			base.OnStop();
		}

		protected override void OnDestroy()
		{
			stopAlarm();
			base.OnDestroy();
		}

		#endregion

		#region Event Handlers

		private void ButtonStopAlarm_Click(object sender, EventArgs e)
		{
			stopAlarm();
			this.Finish();
		}

		private void stopAlarm()
		{
			if(!wasStopped)
			{
				wasStopped = true;

				if(speechEngine != null && speechEngine.IsSpeaking)
					this.speechEngine.Stop();
				if (timer != null)
					timer.Cancel();
				if (currentMessenger != null)
					currentMessenger.Stop();
				ApplicationState.GetInstance(this).SyncNextAlarm();
			}
		}

		public void OnInit(OperationResult status)
		{
			Settings.AddLogMessage(this, "Voice is ready at {0}", DateTime.Now.ToString(ApplicationState.DATE_TIME_TIME_OF_DAY_FORMAT_STRING));
			voiceReady = true;
			speak();
		}

		private void speak()
		{
			if(voiceReady && timeDone)
			{
				// Occurs when the speech engine is initialized
				System.Diagnostics.Debug.Assert(currentMessenger == null);

				currentMessenger = MessengerFactory.CreateMessenger(speechEngine, DateTime.Now, this);
				currentMessenger.OnCompleted += CurrentMessenger_OnCompleted;
				currentMessenger.Start();
			}
		}

		private void CurrentMessenger_OnCompleted(object sender, EventArgs e)
		{
			this.stopAlarm();
			this.Finish();
		}

		#endregion
	}
}