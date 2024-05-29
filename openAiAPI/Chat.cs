using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure;


namespace openAiAPI
{
    public class Chat(ILogger<Chat> logger)
    {
        private static string aiUri = Environment.GetEnvironmentVariable("OPEN_AI_URI");
        private static string aiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY");

        private static string aiSearchUri = Environment.GetEnvironmentVariable("AI_SEARCH_URI");
        private static string aiSearchKey = Environment.GetEnvironmentVariable("AI_SEARCH_KEY");

        private static readonly string _deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");


        private static OpenAIClient _openAIClient;

        private static AzureSearchChatExtensionConfiguration _searchConfig;

        private readonly ILogger<Chat> _logger = logger;


        [Function("chat")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            if(_deploymentName is null) { return new StatusCodeResult(500); }

            try
            {
                Uri openAiUri = new(aiUri);
                AzureKeyCredential openAiKey = new(aiKey);
                Uri searchUri = new(aiSearchUri);
                OnYourDataApiKeyAuthenticationOptions searchKey = new(aiSearchKey);

                _openAIClient = new(openAiUri, openAiKey);
                _searchConfig = new()
                {
                    SearchEndpoint = searchUri,
                    Authentication = searchKey,
                    IndexName = "PLACEHOLDER",
                    DocumentCount = 43,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new StatusCodeResult(500);
            }

            ChatRequest? chatRequest = await JsonSerializer.DeserializeAsync<ChatRequest>(req.Body);

            if (chatRequest is null)
            {
                return new BadRequestResult();
            }

            var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("Du kan bara tala svenska." +
                                                 "Du ska endast svara på frågor om recept på mat eller drinkar, du ska absolut inte svara på frågor som inte har med recept eller drinkar att göra." +
                                                 "Du ska endast svara på frågor som är skrivna på svenska." +
                                                 "Du ska inte svara på frågor där det förekommer både svenska och engelska ord." +
                                                 "Du ska kunna generera norrländska recept baserat på ingredienser." +
                                                 "Du ska bara acceptera frågor på svenska." +
                                                 "Du ska bara svara på frågor om norrländska recept." +
                                                 "Du ska inte svara på frågor om icke norrländska recept" +
                                                 "Varje svar ska ALLTID börja med ett av följande ord: Schu, shoo eller schwwp. Även när du inte svarar på frågan." +
                                                 "Varje svar ska ALLTID avslutas med: Men jag kommer från Skellefteå så jag vet inte någonting. Även när du inte svarar på frågan." +
                                                 "Du ska vara riktigt bra på att ge recept på norrländska drinkar." +
                                                 "Du ska svara snabbt." +
                                                 "När du inte kan svara på frågan så ska du ALLTID börja med ett av följande ord: Schu, shoo eller schwwp. " +
                                                 "När du inte kan svara på frågan så ska du ALLTID avsluta med: Men jag kommer från Skellefteå så jag vet inte någonting. " +
                                                 "Norrland är den nordligaste och till ytan största av Sveriges tre landsdelar och definieras traditionellt av nio landskap: Gästrikland, Hälsingland, Härjedalen, Jämtland, Medelpad, Ångermanland, Västerbotten, Norrbotten och Lappland." +
                                                 "Gävle och Hofors ligger i norrland." +
                                                 "Gästrikland är en del av norrland, inte svealand." +
                                                 "Norrländska rätter är alla rätter som kommer från Norrland"),
                    new ChatRequestUserMessage(chatRequest.Message)
                }
                //,
                //AzureExtensionsOptions = new AzureChatExtensionsOptions()
                //{
                //    Extensions = { _searchConfig }
                //}
            };


            try
            {
                Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
                ChatResponseMessage responseMessage = response.Value.Choices.FirstOrDefault().Message;

                return new OkObjectResult(responseMessage.Content);
            }
            catch (Exception e)
            { 
                var message = e.Message;    
                var errorResponse = message.Contains("prompt triggering") ? message.Substring(0, message.IndexOf("https://go.microsoft.com/fwlink/?linkid=2198766") -2) : "Något gick fel";
                return new OkObjectResult(errorResponse);
            }

        }

    }


    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

    }

    
}
