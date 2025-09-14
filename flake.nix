{
  description = "Vintage Story Volumetric Shading Refresh";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs {
          inherit system;
          config.allowUnfree = true; # In case any tools need unfree packages
        };

        vintageStory = pkgs.vintagestory;

        # Custom script to set up Vintage Story environment
        setupVintageStory = pkgs.writeScriptBin "setup-vintage-story" ''
          #!${pkgs.bash}/bin/bash
          echo "Setting up Vintage Story development environment..."

          # Check if VINTAGE_STORY is set
          if [ -z "$VINTAGE_STORY" ]; then
            echo "Please set VINTAGE_STORY environment variable to your Vintage Story installation path"
            echo "Example: export VINTAGE_STORY=\"/home/user/.local/share/Steam/steamapps/common/VintageStory\""
            echo "Or add it to your .envrc file"
          else
            echo "VINTAGE_STORY is set to: $VINTAGE_STORY"
            if [ ! -d "$VINTAGE_STORY" ]; then
              echo "Warning: VINTAGE_STORY directory does not exist!"
            else
              echo "✓ Vintage Story directory found"
              if [ -f "$VINTAGE_STORY/VintagestoryAPI.dll" ]; then
                echo "✓ VintagestoryAPI.dll found"
              else
                echo "Warning: VintagestoryAPI.dll not found in VINTAGE_STORY directory"
              fi
            fi
          fi

          echo ""
          echo "Available commands:"
          echo "  dotnet build                 - Build the mod"
          echo "  dotnet build --configuration Release - Build for release"
          echo "  setup-vintage-story          - Show this help"
          echo ""
        '';

        # Build script for the mod
        buildMod = pkgs.writeScriptBin "build-mod" ''
          #!${pkgs.bash}/bin/bash
          set -e

          echo "Building Vintage Story FSR2 Mod..."

          if [ -z "$VINTAGE_STORY" ]; then
            echo "Error: VINTAGE_STORY environment variable not set!"
            echo "Set it to your Vintage Story installation directory."
            exit 1
          fi

          # Build the project
          dotnet build --configuration Release

          if [ $? -eq 0 ]; then
            echo "✓ Build successful!"
            echo ""
            echo "Built files should be in: bin/Release/net8.0/"
            echo "Copy the built mod to your Vintage Story mods folder:"
            echo "  ~/.config/VintagestoryData/Mods/"
          else
            echo "✗ Build failed!"
            exit 1
          fi
        '';

      in
      {
        devShells.default = pkgs.mkShell {
          env = {
            VINTAGE_STORY = "${vintageStory}/share/vintagestory";
          };
          buildInputs = with pkgs; [
            # .NET Development - Updated for VS 1.21.0
            dotnet-sdk_8
            dotnet-runtime_8
            msbuild
            vintageStory

            # Development Tools
            omnisharp-roslyn # C# language server

            # General Development Utilities
            git
            curl
            wget
            unzip

            # Code Quality Tools
            nixpkgs-fmt # For formatting nix files

            # Custom Scripts
            setupVintageStory
            buildMod
          ];

          shellHook = ''
            echo "🎮 Vintage Story FSR2 Mod Development Environment"
            echo "================================================"

            # Set up .NET environment
            export DOTNET_ROOT="${pkgs.dotnet-sdk_8}"
            export PATH="$DOTNET_ROOT/bin:$PATH"

            # Disable .NET telemetry
            export DOTNET_CLI_TELEMETRY_OPTOUT=1

            # Set up NuGet packages location
            export NUGET_PACKAGES="$PWD/.nuget/packages"

            # Create .nuget directory if it doesn't exist
            mkdir -p .nuget/packages

            # Show environment info
            echo "📦 .NET SDK Version: $(dotnet --version)"
            echo "🔧 MSBuild Version: $(msbuild -version | head -n1)"
            echo ""

            # Run setup script
            setup-vintage-story
          '';

          # Environment variables that persist in the shell
          DOTNET_CLI_TELEMETRY_OPTOUT = "1";
          DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1";
        };

        # Optional: Add packages that can be built/run directly
        packages = {
          # You could add a package definition here if you want to build the mod as a package
          inherit setupVintageStory buildMod;
        };

        # Default package
        defaultPackage = setupVintageStory;
      }
    );
}
