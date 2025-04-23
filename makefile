# Makefile equivalent of the Cake script

# -----------------------------------------------------------------------------
# Variables
# -----------------------------------------------------------------------------

# Configuration (Release/Debug). Can be overridden from command line: make CONFIGURATION=Debug
CONFIGURATION ?= Release

# Solution and Project Paths (Adjust as necessary)
SOLUTION         = ./VaultSharp.Extensions.Configuration.sln
TEST_PROJECT     = ./Tests/VaultSharp.Extensions.Configuration.Test/VaultSharp.Extensions.Configuration.Test.csproj
PROJECT_TO_PUBLISH = ./Source/VaultSharp.Extensions.Configuration/VaultSharp.Extensions.Configuration.csproj

# Output Directories
PUBLISH_DIR      = .\publish

# Version Info (Consider making this dynamic, e.g., from git tag or a file)
VERSION          = 1.0.0

# NuGet Settings
NUGET_SOURCE     = https://api.nuget.org/v3/index.json
# Allow NUGET_API_KEY to be passed via environment or command line
NUGET_API_KEY    ?= $(NUGET_API_KEY) # Uses env var NUGET_API_KEY if set, otherwise empty

# Tools (using system dotnet)
DOTNET           = dotnet

# Shell settings (use bash for more features if needed)
SHELL            = /bin/sh

# -----------------------------------------------------------------------------
# Cross-platform Shell Helper
# -----------------------------------------------------------------------------
# Use 'pwsh' if available, otherwise fallback to 'powershell' on Windows
ifeq ($(OS),Windows_NT)
	SHELL := pwsh.exe
	POWERSHELL := pwsh.exe
	MD = if (!(Test-Path '$(PUBLISH_DIR)')) { New-Item -ItemType Directory -Path '$(PUBLISH_DIR)' | Out-Null }
	RM = if (Test-Path '$(PUBLISH_DIR)') { Remove-Item -Recurse -Force '$(PUBLISH_DIR)' }
	LS = Test-Path "$(PUBLISH_DIR)\*.nupkg"
else
	SHELL := /bin/bash
	POWERSHELL := pwsh
	MD = mkdir -p $(PUBLISH_DIR)
	RM = rm -rf $(PUBLISH_DIR)
	LS = ls $(PUBLISH_DIR)/*.nupkg 1> /dev/null 2>&1
endif

# -----------------------------------------------------------------------------
# Phony Targets (Targets that don't represent files)
# -----------------------------------------------------------------------------
.PHONY: all Default Clean Restore Build Test Pack Push help

# -----------------------------------------------------------------------------
# Default Target
# -----------------------------------------------------------------------------
# The first target is the default if none is specified on the command line.
# Maps to Cake's "Default" task which depends on "Publish".
Default: Pack

all: Default # Common alias for the main default action

# -----------------------------------------------------------------------------
# Build Tasks (Mapped from Cake Tasks)
# -----------------------------------------------------------------------------

# Clean build artifacts and publish directory
Clean:
	@echo "--- Cleaning ---"
	@echo "Removing Directory: $(PUBLISH_DIR)"
	rm -rf $(PUBLISH_DIR)
	$(DOTNET) clean $(SOLUTION) --configuration $(CONFIGURATION)
	@echo "Clean complete."

# Restore NuGet packages
Restore:
	@echo "--- Restoring Dependencies ---"
	$(DOTNET) restore $(SOLUTION)
	@echo "Restore complete."

# Build the solution
# Depends on Restore (Make handles dependency order)
Build: Restore
	@echo "--- Building Solution (Version: $(VERSION), Configuration: $(CONFIGURATION)) ---"
	$(DOTNET) build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore /p:Version=$(VERSION)
	@echo "Build complete."

# Run tests
# Depends on Build
Test: Build
	@echo "--- Running Tests ---"
	$(DOTNET) test $(TEST_PROJECT) \
		--configuration $(CONFIGURATION) \
		--no-build --no-restore \
		--collect:"XPlat Code Coverage" \
		--logger "trx" \
		--logger "html;LogFileName=VaultSharp.Extensions.Configuration.Test.html" \
		--results-directory ./coverage \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=opencover \
		/p:CoverletOutput=coverage.cobertura.xml
	@echo "Tests complete."

# Create NuGet packages
# Depends on Build
Pack: Build
	@echo "--- Packing NuGet Packages (Version: $(VERSION)) ---"
	$(DOTNET) --version > /dev/null 2>&1 && (mkdir -p $(PUBLISH_DIR)) || (powershell -Command "if (!(Test-Path '$(PUBLISH_DIR)')) { New-Item -ItemType Directory -Path '$(PUBLISH_DIR)' | Out-Null }")
	$(DOTNET) pack $(SOLUTION) --configuration $(CONFIGURATION) --no-build --no-restore -o $(PUBLISH_DIR) /p:PackageVersion=$(VERSION) /p:Version=$(VERSION)
	@echo "Packaging complete. Packages in $(PUBLISH_DIR)"

# Push NuGet packages to the source
# Depends on Pack
Push: Pack
	@echo "--- Pushing NuGet Packages ---"
ifeq ($(OS),Windows_NT)
	$(DOTNET) nuget push "$(PUBLISH_DIR)\*.nupkg" --source $(NUGET_SOURCE) --api-key $(NUGET_API_KEY);
else
	$(DOTNET) nuget push $(PUBLISH_DIR)/*.nupkg --source $(NUGET_SOURCE) --api-key $(NUGET_API_KEY);
endif
	@echo "Push attempt finished."
	

# -----------------------------------------------------------------------------
# Help Target
# -----------------------------------------------------------------------------
help:
	@echo "Usage: make [TARGET] [VARIABLE=VALUE...]"
	@echo ""
	@echo "Targets:"
	@echo "  all          (Default) Clean, restore, build, test, pack, and publish"
	@echo "  Clean        Clean build artifacts and publish directory"
	@echo "  Restore      Restore NuGet packages"
	@echo "  Build        Build the solution"
	@echo "  Test         Run unit tests"
	@echo "  Pack         Create NuGet packages"
	@echo "  Push         Push NuGet packages to the source (requires NUGET_API_KEY)"
	@echo ""
	@echo "Variables:"
	@echo "  CONFIGURATION  (Default: $(CONFIGURATION)) Build configuration (e.g., Debug, Release)"
	@echo "  VERSION        (Default: $(VERSION))     Version for build/pack/publish"
	@echo "  NUGET_API_KEY  (Required for Push)   API key for NuGet source"
	@echo ""
	@echo "Example:"
	@echo "  make Test CONFIGURATION=Debug"
	@echo "  make Push NUGET_API_KEY=xyz..."