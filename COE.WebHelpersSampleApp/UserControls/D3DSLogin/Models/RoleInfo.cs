namespace COE.WebHelpersSampleApp.UserControls.D3DSLogin.Models
{
    public class RoleInfo
    {
        public string Name { get; set; }
        public string NLSName { get; set; }

        public RoleInfo(string name, string nlsName)
        {
            Name = name;
            NLSName = nlsName;
        }

        // Override ToString to display NLS name in ComboBox
        public override string ToString()
        {
            return NLSName;
        }
    }
}
