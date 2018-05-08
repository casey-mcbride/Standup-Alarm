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

namespace StandupAlarm.Models.StandupMessengers
{
	static class MessengerFactory
	{
		public static IStandupMessenger CreateMessenger(TextToSpeech speechEngine, DateTime date)
		{
			// TODO: Put date logic here for picking special messengers

			return createRandomPhraseMessenger(speechEngine);
		}

		private static IStandupMessenger createRandomPhraseMessenger(TextToSpeech speechEngine)
		{
			string[] phrases = new string[]
			{
				"Standard Derp",
				"Stand Up Time",
			};

			Random r = new Random();
			int random = r.Next(0, phrases.Length);
			string phrase = phrases[random];

			return new SimplePhraseMessenger(speechEngine, phrase);
		}
	}
}