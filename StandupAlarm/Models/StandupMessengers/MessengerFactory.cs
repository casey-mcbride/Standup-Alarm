using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using StandupAlarm.Persistance;

namespace StandupAlarm.Models.StandupMessengers
{
	static class MessengerFactory
	{
		private const int NUM_REPEATS = 5;
		
		public static IStandupMessenger CreateMessenger(TextToSpeech speechEngine, DateTime date, Context context)
		{
			// TODO: Put date logic here for picking special messengers

			string oneTimeMessage = Settings.GetOneOffMessage(context);
			if(!string.IsNullOrEmpty(oneTimeMessage))
			{
				Settings.SetOneOffMessage(string.Empty, context);
				return new SimplePhraseMessenger(speechEngine, oneTimeMessage, NUM_REPEATS);
			}

			return createRandomPhraseMessenger(speechEngine);
		}

		private static IStandupMessenger createRandomPhraseMessenger(TextToSpeech speechEngine)
		{
			string[] phrases = new string[]
			{
				"Standard Derp",
				"Stand Up Time",
				"Derp derp time",
				"Derp derp derp derp",
				"Get up, Bryce",
				"It's Casey time",
				"standadarerp",
				"The cow goes moo",
				"Insert message here",
				"Screw up time",
				"This sequence cannot be aborted",
			};

			Random r = new Random();
			int random = r.Next(0, phrases.Length);
			string phrase = phrases[random];

			return new SimplePhraseMessenger(speechEngine, phrase, NUM_REPEATS);
		}
	}
}