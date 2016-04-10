//  Copyright 2015 Stefan Negritoiu (FreeBusy). See LICENSE file for more information.

using System;
using System.Collections.Generic;
using NLog;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.Slu;
using AlexaSkillsKit.UI;
using System.Data.Linq;
using System.Linq;





namespace Sample.Controllers
{
    public class SampleSessionSpeechlet : Speechlet
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();

        // Note: NAME_KEY being a JSON property key gets camelCased during serialization
        private const string NAME_KEY = "name";
        private const string NAME_SLOT = "Name";
        private const string MODEL_KEY = "MODEL_TYPE";
        private const string MODEL_SLOT = "modelname";


        public override void OnSessionStarted(SessionStartedRequest request, Session session) {            
            Log.Info("OnSessionStarted requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
        }


        public override SpeechletResponse OnLaunch(LaunchRequest request, Session session) {
            Log.Info("OnLaunch requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
            return GetWelcomeResponse();
        }

       

        public override SpeechletResponse OnIntent(IntentRequest request, Session session) {
            Log.Info("OnIntent requestId={0}, sessionId={1}", request.RequestId, session.SessionId);

            // Get intent from the request object.
            Intent intent = request.Intent;
            string intentName = (intent != null) ? intent.Name : null;

            // Note: If the session is started with an intent, no welcome message will be rendered;
            // rather, the intent specific response will be returned.
            if ("MyNameIsIntent".Equals(intentName)) {
                return SetNameInSessionAndSayHello(intent, session);
            } 
            else if ("WhatsMyNameIntent".Equals(intentName)) {
                return GetNameFromSessionAndSayHello(intent, session);
            }
            else if ("statusIntent".Equals(intentName))
            {
                return StatusUpdate(intent, session);
            }
            else if ("modelIntent".Equals(intentName))
            {
                return ModelInfo(intent, session);
            }
            else {
                throw new SpeechletException("Invalid Intent");
            }
        }


        public override void OnSessionEnded(SessionEndedRequest request, Session session) {
            Log.Info("OnSessionEnded requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
        }


        /**
         * Creates and returns a {@code SpeechletResponse} with a welcome message.
         * 
         * @return SpeechletResponse spoken and visual welcome message
         */
        private SpeechletResponse GetWelcomeResponse() {
            // Create a random numnber. Right now I am hard coding.. eventually we will pull this from the configuration database. 


            string speechOutput = "Welcome to the Star Citizen Computer helper system. ";

            return BuildSpeechletResponse("Welcome", speechOutput, false);
            

         
           
        }

        /**
         * Creates and returns a {@code SpeechletResponse} with a status message.
         * 
         * @return SpeechletResponse spoken and visual status message
         */
        private SpeechletResponse StatusUpdate(Intent intent, Session session)
        {
            // Create a random numnber. Right now I am hard coding.. eventually we will pull this from the configuration database. 

            using (var sc1 = new StarCitizenDBEntities10())
            {
                var query1 = from i in sc1.Configs
                            where i.Name.Equals("NumberOfStatusMessages")
                            select i;

                var config = query1.First();
                //set the dialogue to the speechOutput. 
                int range = Convert.ToInt16(config.Value);




                Random rnd = new Random();
                int x = rnd.Next(1, range);

                //pull the dialogue from the database 

                using (var sc = new StarCitizenDBEntities10())
                {
                    var query = from i in sc.StatusSCs
                                where i.Id.Equals(x)
                                select i;
                    var status = query.First();


                    //set the dialogue to the speechOutput. 

                    string speechOutput = status.Dialogue.ToString();
                    return BuildSpeechletResponse("Status Update", speechOutput, false);
                }
            }
        }


        private SpeechletResponse ModelInfo(Intent intent, Session session)
        {
            // Get the slots from the intent.
            Dictionary<string, Slot> slots = intent.Slots;

            // Get the model slot from the list slots.
            Slot modelSlot = slots[MODEL_SLOT];
            string speechOutput = "";

            // Check for name and create output to user.
            if (modelSlot != null)
            {
                // Store the user's name in the Session and create response.
                string modelname = modelSlot.Value;
                session.Attributes[MODEL_KEY] = modelname;

                //retrieve model dialogue from the database

                using (var sc = new StarCitizenDBEntities10())
                {
                    var query = from i in sc.ModelSCs
                                where i.ModelName.Equals(modelname)
                                select i;

                    var model = query.First();

                    //set the dialogue to the speechOutput. 
                    speechOutput = model.ModelDialogue.ToString();
                    return BuildSpeechletResponse("Model Information", speechOutput, false);
                }

            }
            else {
                // Render an error since we don't know what the model name is.
                speechOutput = "I'm not sure which model you wanted information for, please try again";
                return BuildSpeechletResponse("Model Information", speechOutput, false);
            }

        }


        /**
         * Creates a {@code SpeechletResponse} for the intent and stores the extracted name in the
         * Session.
         * 
         * @param intent
         *            intent for the request
         * @return SpeechletResponse spoken and visual response the given intent
         */
        private SpeechletResponse SetNameInSessionAndSayHello(Intent intent, Session session) {
            // Get the slots from the intent.
            Dictionary<string, Slot> slots = intent.Slots;

            // Get the name slot from the list slots.
            Slot nameSlot = slots[NAME_SLOT];
            string speechOutput = "";

            // Check for name and create output to user.
            if (nameSlot != null) {
                // Store the user's name in the Session and create response.
                string name = nameSlot.Value;
                session.Attributes[NAME_KEY] = name;
                speechOutput = String.Format(
                    "Hello {0}, now I can remember your name, you can ask me your name by saying, whats my name?", name);
            } 
            else {
                // Render an error since we don't know what the users name is.
                speechOutput = "I'm not sure what your name is, please try again";
            }

            // Here we are setting shouldEndSession to false to not end the session and
            // prompt the user for input
            return BuildSpeechletResponse(intent.Name, speechOutput, false);
        }


        /**
         * Creates a {@code SpeechletResponse} for the intent and get the user's name from the Session.
         * 
         * @param intent
         *            intent for the request
         * @return SpeechletResponse spoken and visual response for the intent
         */
        private SpeechletResponse GetNameFromSessionAndSayHello(Intent intent, Session session) {
            string speechOutput = "";
            bool shouldEndSession = false;

            // Get the user's name from the session.
            string name = (String)session.Attributes[NAME_KEY];

            // Check to make sure user's name is set in the session.
            if (!String.IsNullOrEmpty(name)) {
                speechOutput = String.Format("Your name is {0}, goodbye", name);
                shouldEndSession = true;
            } 
            else {
                // Since the user's name is not set render an error message.
                speechOutput = "I'm not sure what your name is, you can say, my name is Sam";
            }

            return BuildSpeechletResponse(intent.Name, speechOutput, shouldEndSession);
        }


        /**
         * Creates and returns the visual and spoken response with shouldEndSession flag
         * 
         * @param title
         *            title for the companion application home card
         * @param output
         *            output content for speech and companion application home card
         * @param shouldEndSession
         *            should the session be closed
         * @return SpeechletResponse spoken and visual response for the given input
         */
        private SpeechletResponse BuildSpeechletResponse(string title, string output, bool shouldEndSession) {
            // Create the Simple card content.
            SimpleCard card = new SimpleCard();
            card.Title = String.Format(title);
            card.Subtitle = String.Format("Star Citizen");
            card.Content = String.Format(output);

            // Create the plain text output.
            PlainTextOutputSpeech speech = new PlainTextOutputSpeech();
            speech.Text = output;

            // Create the speechlet response.
            SpeechletResponse response = new SpeechletResponse();
            response.ShouldEndSession = shouldEndSession;
            response.OutputSpeech = speech;
            response.Card = card;
            return response;
        }
    }
}