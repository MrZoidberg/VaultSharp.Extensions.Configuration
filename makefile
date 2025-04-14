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
PUBLISH_DIR      = ./publish
PUBLISH_APP_DIR  = $(PUBLISH_DIR)/App # Specific dir for the published app

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
# Phony Targets (Targets that don't represent files)
# -----------------------------------------------------------------------------
.PHONY: all Default Clean Restore Build Test Pack Publish Push help

# -----------------------------------------------------------------------------
# Default Target
# -----------------------------------------------------------------------------
# The first target is the default if none is specified on the command line.
# Maps to Cake's "Default" task which depends on "Publish".
Default: Publish

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
	$(DOTNET) test $(TEST_PROJECT) --configuration $(CONFIGURATION) --no-build --no-restore --collect:"XPlat Code Coverage" --logger:trx \
	--logger html;LogFileName=VaultSharp.Extensions.Configuration.Test.html /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=coverage.cobertura.xml --results-directory ./coverage \
	-p:CoverletOutput=coverage.cobertura.xml
	@echo "Tests complete."

# Create NuGet packages
# Depends on Build
Pack: Build
	@echo "--- Packing NuGet Packages (Version: $(VERSION)) ---"
	# Ensure publish dir exists for output
	mkdir -p $(PUBLISH_DIR)
	$(DOTNET) pack $(SOLUTION) --configuration $(CONFIGURATION) --no-build --no-restore -o $(PUBLISH_DIR) /p:PackageVersion=$(VERSION) /p:Version=$(VERSION)
	@echo "Packaging complete. Packages in $(PUBLISH_DIR)"

# Publish the application
# Depends on Test (ensures tests pass before publishing)
# Might also depend on Pack if publish consumes packed artifacts, but typically depends on Build/Test.
Publish: Test
	@echo "--- Publishing Application (Version: $(VERSION)) ---"
	# Ensure publish app dir exists
	mkdir -p $(PUBLISH_APP_DIR)
	$(DOTNET) publish $(PROJECT_TO_PUBLISH) --configuration $(CONFIGURATION) --no-build --no-restore -o $(PUBLISH_APP_DIR) /p:Version=$(VERSION)
	@echo "Publish complete. Application in $(PUBLISH_APP_DIR)"

# Push NuGet packages to the source
# Depends on Pack
Push: Pack
	@echo "--- Pushing NuGet Packages ---"
	# Check if NUGET_API_KEY is set (Makefile equivalent of .WithCriteria)
	$(if $(NUGET_API_KEY), \
		find $(PUBLISH_DIR) -name "*.nupkg" -exec $(DOTNET) nuget push {} --api-key $(NUGET_API_KEY) --source $(NUGET_SOURCE) \; , \
		@echo "NUGET_API_KEY not set. Skipping push. Set it via environment or 'make Push NUGET_API_KEY=your_key'" \
	)
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
	@echo "  Publish      Publish the application"
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