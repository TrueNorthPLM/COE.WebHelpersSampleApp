using COE.WebHelpersSampleApp.Commands;
using COE.WebHelpersSampleApp.Helpers;
using COE.WebHelpersSampleApp.Messaging;
using Serilog;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using TrueNorth.D3DS.WebServices.Core.Helpers;
using TrueNorth.D3DS.WebServices.Core.Models;
using TrueNorth.Shared.Models.DTOs.BOMSight;

namespace COE.WebHelpersSampleApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ICommand PopulateTreeCommand => new CommandsBase(_populateTreeExecute);

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private string _partNumber = "prd-R1132102979512-00000003"; 
        public string PartNumber
        {
            get => _partNumber;
            set
            {
                _partNumber = value;
                OnPropertyChanged();
            }
        }

        private string _revision = "A";
        public string Revision
        {
            get => _revision;
            set
            {
                _revision = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoggedIn => _passportLoginHelper != null;
        public ObservableCollection<ProductNode> RootNodeChildren { get; set; } = new();

        private PassportLoginHelper? _passportLoginHelper = null;
        private readonly ILogger _logger;
        private readonly IViewModelMessenger _messenger;
        public MainViewModel(ILogger logger, IViewModelMessenger messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // subscribe to the UserCredentials message
            _messenger.Subscribe<UserCredentials>(this, OnUserCredentialsReceived);
        }

        private void OnUserCredentialsReceived(object? message)
        {
            // set the passport login helper
            if (message is UserCredentials userCredentials)
            {
                _passportLoginHelper = userCredentials.PassportLoginHelper;
                OnPropertyChanged(nameof(IsLoggedIn));
            }
        }

        private async void _populateTreeExecute(object parameter)
        {
            IsBusy = true;
            RootNodeChildren = new ObservableCollection<ProductNode>()
            {
                await GetProductTreeByPartNumberAndRevisionAsync(PartNumber, Revision)
            };
            OnPropertyChanged(nameof(RootNodeChildren));
            IsBusy = false;
        }

        public async Task<ProductNode> GetProductTreeByIdAsync(string rootEngItemObjectId)
        {
            HttpClient client = _passportLoginHelper.HttpClient;
            string baseUrl = _passportLoginHelper.SpaceUrl;
            string requestUrl = $"{baseUrl}/resources/v1/modeler/dseng/dseng:EngItem/{rootEngItemObjectId}/expand";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("SecurityContext", _passportLoginHelper.SelectedSecurityContext);
            request.Headers.Add("ENO_CSRF_TOKEN", _passportLoginHelper.CsrfToken);
            request.Headers.Add("Accept", "application/json");

            StringContent content = new StringContent(
                $$"""
                {
                  "expandDepth": -1,
                  "withPath": true,
                  "type_filter_bo": [
                    "VPMReference"
                  ],
                  "type_filter_rel": [
                    "VPMInstance"
                  ]
                }
                """,
                null,
                "application/json");

            request.Content = content;
            var response = await client.SendAsync(request);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            string jsonString = await response.Content.ReadAsStringAsync();

            var responseBody = JsonSerializer.Deserialize<ProductStructureResponse>(jsonString);

            // Create the builder and build the hierarchy
            var builder = new ProductHierarchyBuilder();
            var rootNode = builder.BuildHierarchy(responseBody);

            return rootNode;
        }

        public async Task<ProductNode> GetProductTreeByPartNumberAndRevisionAsync(
            string rootPartNumber, string rootRevision)
        {
            string objectId = await GetObjectIdForPartNumberAndRevisionAsync(rootPartNumber, rootRevision);
            return await GetProductTreeByIdAsync(objectId);
        }

        public async Task<string> GetObjectIdForPartNumberAndRevisionAsync(string partNumber, string revision)
        {
            HttpClient client = _passportLoginHelper.HttpClient;
            string baseUrl = _passportLoginHelper.SpaceUrl;
            string requestUrl = $"{baseUrl}/resources/v1/modeler/dseng/dseng:EngItem/search?$mask=dskern:Mask.Default&$searchStr=name:{partNumber} AND revision:{revision}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("SecurityContext", _passportLoginHelper.SelectedSecurityContext);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            // verify we only have 1 result
            int totalItems = root.GetProperty("totalItems").GetInt32();
            if (totalItems == 0)
                throw new Exception($"nothing found for criteria {partNumber} {revision}");
            if (totalItems > 1)
                throw new Exception($"multiple results found for criteria {partNumber} {revision}");

            // okay, we've verified that there is exactly 1 search result.  let's return its ID
            return root.GetProperty("member")[0].GetProperty("id").GetString();
        }
    }
}
