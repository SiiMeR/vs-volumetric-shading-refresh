dotnet run --project ./CakeBuild/CakeBuild.csproj -- "$@"

nix run nixpkgs#zip -- -r ../Releases/VolumetricShadingRefreshed-$(date +%Y%m%d).zip VolumetricShadingRefreshed/bin/Release/Mods/mod/publish/*