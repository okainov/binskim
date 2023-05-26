#!/bin/bash

echo $(uname)
if [[  "$(uname)" == "Linux" || "$(uname)" == "Darwin" ]]; then
  echo "Changing paths in BinSkim SLN to non-Windows paths due to msbuild issue #1957 (https://github.com/microsoft/msbuild/issues/1957)"
  sed 's#\\#/#g' src/BinSkim.sln > src/BinSkimUnix.sln
fi

if [ ! -f src/sarif-sdk/src/Sarif.Sdk.sln ]; then
  echo "Get submodule..."
  git submodule update --init --recursive
fi

dotnet nuget locals all --clear
nuget locals all -clear
dotnet build src/BinSkimUnix.sln --configuration Release /p:Platform="x64" --packages src/packages

dotnet test bld/bin/x64_Release/netcoreapp3.1/Test.FunctionalTests.BinSkim.Driver.dll
dotnet test bld/bin/x64_Release/netcoreapp3.1/Test.FunctionalTests.BinSkim.Rules.dll
dotnet test bld/bin/x64_Release/netcoreapp3.1/Test.UnitTests.BinaryParsers.dll
dotnet test bld/bin/x64_Release/netcoreapp3.1/Test.UnitTests.BinSkim.Rules.dll