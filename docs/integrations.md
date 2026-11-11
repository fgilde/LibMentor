# CI integration

Keep your library's configuration, tool manifest and deployment workflow in its own repository. LibMentor does not need a copy of your repository, credentials for an AI model or a hosted service.

## Configure a local tool

Create a tool manifest if the repository does not already have one, then pin the package version:

~~~sh
dotnet new tool-manifest
dotnet tool install LibMentor --version 0.1.1
dotnet tool run libmentor -- init
~~~

Review the generated libmentor.json. Set the library name, package version, repository URL, documentation globs, source globs and the public baseUrl of the generated directory. Commit the configuration and .config/dotnet-tools.json.

Add the generated directory to .gitignore:

~~~gitignore
.libmentor/
~~~

## Generate and check in GitHub Actions

The following workflow uses the local tool manifest. Use your repository's default branch in the push filter.

~~~yaml
name: AI documentation
on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:
permissions:
  contents: read
jobs:
  documentation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet tool restore
      - name: Generate documentation
        run: dotnet tool run libmentor -- generate --revision "$GITHUB_SHA"
      - name: Verify deterministic output
        run: dotnet tool run libmentor -- generate --check --revision "$GITHUB_SHA"
      - uses: actions/upload-artifact@v4
        with:
          name: ai-documentation
          path: .libmentor
          include-hidden-files: true
~~~

Generation validates configured inputs before exporting. The check command regenerates in memory and fails if the existing output differs. Artifact upload retains the result for inspection; public hosting requires a deployment step.

When the package version is resolved by an existing release script, pass the same value with --library-version. For a source build, append a commit suffix to distinguish it from an actual published package version.

## Add the output to your website

Generate documentation before your existing website upload, then copy the complete .libmentor directory into a dedicated output subdirectory such as site/ai. Set baseUrl to that exact HTTPS directory.

Publish the merged website output once. Avoid adding a competing deployment that replaces the existing site. Keep all skill references, document files, API metadata and the ownership manifest together.

For a new GitHub Pages site, choose GitHub Actions as its publishing source and deploy the assembled site with actions/upload-pages-artifact and actions/deploy-pages. Existing static-site generators can copy the output into their own publish directory.

## Other input formats

C# source declarations are parsed without loading a complete project. Supply compiler XML files through xmlDocumentation when built API documentation is needed, including Razor-generated APIs.

Generate TypeDoc reflection JSON with your TypeScript project's existing TypeDoc setup, then add its paths under typeDoc. LibMentor consumes that JSON; it does not install or execute a TypeScript toolchain.

Standalone .csproj and .fsproj paths under examples are compiled during validation. Keep example builds independent of production services and deployment credentials.

## Version snapshots

The output includes a current snapshot and versions/<version>/. Retain earlier version folders in the publishing pipeline if historical documentation must remain available; a clean checkout only generates the configured version.

See the [configuration schema](../schemas/configuration.schema.json), [command reference](../README.md#commands) and [release setup](releasing.md).
