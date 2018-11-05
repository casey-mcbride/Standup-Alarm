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

namespace StandupAlarm.Models.StandupMessengers
{
	/// <summary>
	/// Messenger that repeats a simple phrase a couple times.
	/// </summary>
	sealed class SimplePhraseMessenger : UtteranceProgressListener, IStandupMessenger
	{
		#region Constants

		public const float INITIAL_PITCH = .8f;
		public const float PITCH_INCREASE = .45f;
		public const float INITIAL_SPEACH_RATE = .8f;
		public const float SPEACH_RATE_INCREASE = .2f;

		/// <summary>
		/// The pause between saying the phrase.
		/// </summary>
		private static readonly TimeSpan pauseTime = TimeSpan.FromSeconds(1);

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		private string phrase;

		private int numRepeats;

		/// <summary>
		/// ID of the last utterance.
		/// </summary>
		private Guid lastUtteranceID;

		#endregion

		#region Properties

		public event EventHandler OnCompleted;

		#endregion

		#region Initializers

		public SimplePhraseMessenger(TextToSpeech speechEngine, string phrase, int numRepeats)
		{
			this.speechEngine = speechEngine;
			this.phrase = phrase;
			this.numRepeats = numRepeats;
			AudioAttributes.Builder b = new AudioAttributes.Builder();
			b.SetFlags(AudioFlags.LowLatency);
			b.SetUsage(AudioUsageKind.Alarm);
			b.SetContentType(AudioContentType.Speech);

			AudioAttributes audioAttributes = b.Build();

			speechEngine.SetAudioAttributes(audioAttributes);
		}

		#endregion

		#region Methods

		public void Start()
		{
			this.lastUtteranceID = Guid.NewGuid();
			speechEngine.SetOnUtteranceProgressListener(this);

			for (int i = 0; i < numRepeats - 1; i++)
			{
				// Increase the pitch for a comical result
				speechEngine.SetPitch(INITIAL_PITCH + i * PITCH_INCREASE);
				speechEngine.SetSpeechRate(INITIAL_SPEACH_RATE + i * SPEACH_RATE_INCREASE);
				sayThePhrase(Guid.NewGuid());
			}

			speechEngine.SetPitch(INITIAL_PITCH);
			speechEngine.SetSpeechRate(INITIAL_SPEACH_RATE);
			sayThePhrase(lastUtteranceID);
		}

		private void sayThePhrase(Guid id)
		{
			speechEngine.Speak(phrase, QueueMode.Add, new Bundle(), id.ToString());
			speechEngine.PlaySilentUtterance((int)pauseTime.TotalMilliseconds, QueueMode.Add, Guid.NewGuid().ToString());
		}

		public void Stop()
		{
			speechEngine.Stop();
		}

		public override void OnDone(string utteranceId)
		{
			if(utteranceId == lastUtteranceID.ToString())
			{
				var eve = OnCompleted;
				if (eve != null)
					eve(this, EventArgs.Empty);
			}
		}

		public override void OnError(string utteranceId)
		{
		}

		public override void OnStart(string utteranceId)
		{
		}

		#endregion

		#region Event Handlers

		#endregion
	}
}