using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace COE.WebHelpersSampleApp.Helpers
{
    /// <summary>
    /// Interface defining the contract for 3DEXPERIENCE Platform Passport authentication and security context management.
    /// </summary>
    public interface IPassportLoginHelper
    {
        /// <summary>
        /// Gets the list of available security contexts for the authenticated user.
        /// </summary>
        List<string> AvailableSecurityContexts { get; }

        /// <summary>
        /// Gets the cookie container used for managing authentication cookies.
        /// </summary>
        CookieContainer CookieContainer { get; }

        /// <summary>
        /// Gets the CSRF token for the current session.
        /// </summary>
        string CsrfToken { get; }

        /// <summary>
        /// Gets the HttpClient instance used for making HTTP requests.
        /// </summary>
        HttpClient HttpClient { get; }

        /// <summary>
        /// Gets the preferred security context for the authenticated user.
        /// </summary>
        string PreferredSecurityContext { get; }

        /// <summary>
        /// Gets or sets the currently selected security context.
        /// </summary>
        string SelectedSecurityContext { get; set; }

        /// <summary>
        /// Gets the email address of the authenticated user.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Gets the first name of the authenticated user.
        /// </summary>
        string? FirstName { get; }

        /// <summary>
        /// Gets the last name of the authenticated user.
        /// </summary>
        string? LastName { get; }

        /// <summary>
        /// Gets whether the authenticated user account is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the mapping between role names and their NLS (National Language Support) values.
        /// </summary>
        Dictionary<string, string> RoleNLSMappings { get; }

        /// <summary>
        /// Gets the cookie string for a specified domain.
        /// </summary>
        /// <param name="domain">The domain for which to retrieve cookies.</param>
        /// <returns>A string containing all cookies for the specified domain.</returns>
        string GetCookieString(string domain);

        /// <summary>
        /// Checks if the user has write access in a specific collaborative space.
        /// </summary>
        /// <param name="collaborativeSpace">The name of the collaborative space to check.</param>
        /// <returns>True if the user has write access, false otherwise.</returns>
        bool HasWriteAccessInCollaborativeSpace(string collaborativeSpace);

        /// <summary>
        /// Performs the login process to authenticate with the 3DEXPERIENCE Platform.
        /// </summary>
        /// <returns>A task representing the asynchronous login operation.</returns>
        Task LoginAsync();
    }

    /// <summary>
    /// Implements authentication and security context management for the 3DEXPERIENCE Platform Passport.
    /// </summary>
    public class PassportLoginHelper : IPassportLoginHelper
    {
        private readonly string _passportUrl = "https://r1132102979512-eu1.iam.3dexperience.3ds.com";

        /// <summary>
        /// Gets the Space URL for the 3DEXPERIENCE Platform.
        /// </summary>
        public readonly string SpaceUrl = "https://r1132102979512-usw1-space.3dexperience.3ds.com/enovia";

        private readonly string _tenant = "R1132102979512";
        private readonly string _username;
        private readonly string _plainTextPassword;

        private string? _email;
        private string? _firstName;
        private string? _lastName;
        private bool _isActive;

        /// <inheritdoc/>
        public string? Email => _email;

        /// <inheritdoc/>
        public string? FirstName => _firstName;

        /// <inheritdoc/>
        public string? LastName => _lastName;

        /// <inheritdoc/>
        public bool IsActive => _isActive;

        /// <inheritdoc/>
        public virtual HttpClient HttpClient { get; set; }

        /// <inheritdoc/>
        public CookieContainer CookieContainer { get; private set; }

        /// <inheritdoc/>
        public string CsrfToken { get; set; }

        /// <inheritdoc/>
        public string PreferredSecurityContext { get; set; }

        /// <inheritdoc/>
        public virtual List<string> AvailableSecurityContexts { get; set; }

        /// <inheritdoc/>
        public string SelectedSecurityContext { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, string> RoleNLSMappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the PassportLoginHelper class using a SecureString password.
        /// </summary>
        /// <param name="username">The username for authentication.</param>
        /// <param name="securePassword">The password as a SecureString.</param>
        /// <param name="passportUrl">Optional custom passport URL.</param>
        /// <param name="spaceUrl">Optional custom space URL.</param>
        /// <param name="tenant">Optional custom tenant.</param>
        public PassportLoginHelper(string username, System.Security.SecureString securePassword,
            string? passportUrl = null,
            string? spaceUrl = null,
            string? tenant = null)
            : this(username, SecureStringExtensions.ConvertToUnsecureString(securePassword), passportUrl, spaceUrl, tenant)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PassportLoginHelper class using a plain text password.
        /// </summary>
        /// <param name="username">The username for authentication.</param>
        /// <param name="plainTextPassword">The password in plain text.</param>
        /// <param name="passportUrl">Optional custom passport URL.</param>
        /// <param name="spaceUrl">Optional custom space URL.</param>
        /// <param name="tenant">Optional custom tenant.</param>
        public PassportLoginHelper(string username, string plainTextPassword,
            string? passportUrl = null,
            string? spaceUrl = null,
            string? tenant = null)
        {
            if (!string.IsNullOrEmpty(passportUrl))
                _passportUrl = passportUrl;
            if (!string.IsNullOrEmpty(spaceUrl))
                SpaceUrl = spaceUrl;
            if (!string.IsNullOrEmpty(tenant))
                _tenant = tenant;

            _username = username;
            _plainTextPassword = plainTextPassword;
            PreferredSecurityContext = string.Empty;
            AvailableSecurityContexts = new List<string>();
            CsrfToken = string.Empty;

            InitializeHttpClient();
        }

        /// <summary>
        /// Initializes the HttpClient with appropriate settings.
        /// </summary>
        protected virtual void InitializeHttpClient()
        {
            CookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            HttpClient = new HttpClient(handler);
        }

        /// <inheritdoc/>
        public virtual async Task LoginAsync()
        {
            string loginTicket = await GetLoginTicketAsync();
            await PerformCasAuthenticationAsync(loginTicket);
            CsrfToken = await GetCsrfTokenAsync();
            await GetSecurityContextsAsync();
        }

        /// <summary>
        /// Retrieves a login ticket from the authentication service.
        /// </summary>
        /// <returns>The login ticket string.</returns>
        protected virtual async Task<string> GetLoginTicketAsync()
        {
            string url = $"{_passportUrl}/login?action=get_auth_params";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");

            HttpResponseMessage response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("lt").GetString();
        }

        /// <summary>
        /// Performs CAS authentication using the obtained login ticket.
        /// </summary>
        /// <param name="loginTicket">The login ticket to use for authentication.</param>
        protected virtual async Task PerformCasAuthenticationAsync(string loginTicket)
        {
            string url = $"{_passportUrl}/login?service={HttpUtility.UrlEncode(SpaceUrl)}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("tenant", _tenant);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("lt", loginTicket),
                new KeyValuePair<string, string>("username", _username),
                new KeyValuePair<string, string>("password", _plainTextPassword)
            });
            request.Content = content;

            HttpResponseMessage response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Retrieves a CSRF token for the authenticated session.
        /// </summary>
        /// <returns>The CSRF token string.</returns>
        protected virtual async Task<string> GetCsrfTokenAsync()
        {
            string url = $"{SpaceUrl}/resources/v1/application/CSRF?tenant={_tenant}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            HttpResponseMessage response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("csrf").GetProperty("value").GetString();
        }

        /// <summary>
        /// Retrieves and processes security contexts for the authenticated user.
        /// </summary>
        protected virtual async Task GetSecurityContextsAsync()
        {
            string url = $"{SpaceUrl}/resources/modeler/pno/person?current=true&select=preferredcredentials&select=collabspaces&select=firstname&select=lastname&select=email&select=isactive";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            HttpResponseMessage response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string jsonPayload = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonPayload);

            RoleNLSMappings.Clear();

            _firstName = json["firstname"].ToString();
            _lastName = json["lastname"].ToString();
            _email = json["email"].ToString();
            bool.TryParse(json["isactive"].ToString(), out _isActive);

            var preferred = json["preferredcredentials"];
            string preferredCredentials = $"{preferred["role"]["name"]}.{preferred["organization"]["name"]}.{preferred["collabspace"]["name"]}";

            ProcessSecurityContexts(json, preferredCredentials);
        }

        /// <summary>
        /// Processes the security contexts from the JSON response.
        /// </summary>
        /// <param name="json">The JSON object containing security contexts.</param>
        /// <param name="preferredCredentials">The preferred credentials string.</param>
        protected virtual void ProcessSecurityContexts(JObject json, string preferredCredentials)
        {
            var collabspaces = json["collabspaces"] as JArray;
            List<string> availableCredentials = new List<string>();

            if (collabspaces != null)
            {
                foreach (var space in collabspaces)
                {
                    string spaceName = space["name"].ToString();
                    var couples = space["couples"] as JArray;

                    if (couples != null)
                    {
                        foreach (var couple in couples)
                        {
                            ProcessSecurityContext(couple, spaceName, availableCredentials);
                        }
                    }
                }
            }

            PreferredSecurityContext = preferredCredentials;
            AvailableSecurityContexts = availableCredentials;
            SelectedSecurityContext = PreferredSecurityContext;
        }

        /// <summary>
        /// Processes a single security context entry.
        /// </summary>
        /// <param name="couple">The JSON token containing the security context information.</param>
        /// <param name="spaceName">The name of the collaborative space.</param>
        /// <param name="availableCredentials">The list of available credentials to update.</param>
        protected virtual void ProcessSecurityContext(JToken couple, string spaceName, List<string> availableCredentials)
        {
            var role = couple["role"];
            string roleName = role["name"].ToString();
            string roleNLS = role["nls"]?.ToString() ?? roleName;

            if (!RoleNLSMappings.ContainsKey(roleName))
            {
                RoleNLSMappings.Add(roleName, roleNLS);
            }

            string orgName = couple["organization"]["name"].ToString();
            string securityContext = $"{roleName}.{orgName}.{spaceName}";
            availableCredentials.Add(securityContext);
        }

        /// <inheritdoc/>
        public string GetCookieString(string domain)
        {
            return CookieContainer.GetCookieHeader(new Uri(domain));
        }

        /// <inheritdoc/>
        public bool HasWriteAccessInCollaborativeSpace(string collaborativeSpace)
        {
            var securityContexts = AvailableSecurityContexts
                .Select(s => new SecurityContextInfo(s))
                .ToList();

            var rolesWithWriteAccess = new List<string>
            {
                "VPLMCreator",
                "VPLMProjectLeader",
                "VPLMProjectAdministrator"
            };

            return securityContexts.Any(s =>
                s.CollaborativeSpace == collaborativeSpace &&
                rolesWithWriteAccess.Contains(s.Role));
        }
    }

    /// <summary>
    /// Represents parsed information about a security context in the 3DEXPERIENCE Platform.
    /// </summary>
    public class SecurityContextInfo
    {
        /// <summary>
        /// Initializes a new instance of the SecurityContextInfo class.
        /// </summary>
        /// <param name="securityContextString">A period-delimited string containing role, organization, and collaborative space information.</param>
        /// <exception cref="ArgumentException">Thrown when the security context string is not in the correct format.</exception>
        public SecurityContextInfo(string securityContextString)
        {
            var elements = securityContextString.Split('.');
            if (elements.Length != 3)
                throw new ArgumentException("Security context string must be a period-delimited string consisting of 3 elements!");

            Role = elements[0];
            Organization = elements[1];
            CollaborativeSpace = elements[2];
        }

        /// <summary>
        /// Gets the role component of the security context.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// Gets the organization component of the security context.
        /// </summary>
        public string Organization { get; }

        /// <summary>
        /// Gets the collaborative space component of the security context.
        /// </summary>
        public string CollaborativeSpace { get; }
    }
}