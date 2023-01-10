
namespace UpdateVersionInfo
{
    public class CommandLineArguments
    {
        private readonly OptionSet options;
        public bool ShowHelp { get; private set; }
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int? Build { get; private set; }
		/*!!!
        public int? Revision { get; private set; }
        */
		public string VersionCsPath { get; private set; } = string.Empty;
        public string AndroidManifestPath { get; private set; } = string.Empty;
		public string TouchPListPath { get; private set; } = string.Empty;

		private OptionSet Initialize()
        {
            OptionSet options = new()
            {
                {
                    "?", "Shows help/usage information.", h => this.ShowHelp = true
                },
                {
                    "v|major=", "A numeric major version number greater than zero.", (int v) => this.Major = v
                },
                {
                    "m|minor=", "A numeric minor number greater than zero.", (int v) => this.Minor = v
                },
                {
                    "b|build=", "A numeric build number greater than zero.", (int v) => this.Build = v
                },
                /*!!!
                {
                    "r|revision=", "A numeric revision number greater than zero.", (int v) => Revision = v
                },
                */
                {
                    "p|path=", "The path to a C# file to update with version information.", p => this.VersionCsPath = p
                },
                {
                    "a|androidManifest=", "The path to an android manifest file to update with version information.", p => this.AndroidManifestPath = p
                },
                {
                    "t|touchPlist=", "The path to an iOS plist file to update with version information.", p => this.TouchPListPath = p
                }
            };

            return options;
        }

        public CommandLineArguments(IEnumerable<String> args)
        {
			this.Major = 1;
            this.Minor = 0;
            this.options = this.Initialize();
            this.options.Parse(args);
        }

        public void WriteHelp(System.IO.TextWriter writer)
        {
            this.options.WriteOptionDescriptions(writer);
        }
    }
}
