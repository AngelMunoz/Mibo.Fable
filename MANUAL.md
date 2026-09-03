# Manual for the template

If you are looking for documentation about the project, you should refer to the [README.md](./README.md) file.

## Architecture

- `src/Mibo.Fable/` contains the source code of the `Mibo.Fable` package. Sibling packages (for example a future `Mibo.Fable.Threejs`) live next to it under `src/`
- `demo/` contains a demo application that you can use to test your binding. It can also be publish on GitHub pages to have an interactive demo.

The build process is driven by the scripts in [package.json](./package.json), using pnpm:

| Script             | Description                                                                          |
| ------------------ | ------------------------------------------------------------------------------------ |
| `pnpm install`     | Installs the node dependencies and restores the local dotnet tools (via `postinstall`) |
| `pnpm build`       | Compiles the library to JavaScript (`src/Mibo.Fable/Mibo.Fable.fs.js` + `src/Mibo.Fable/fable_modules/`)    |
| `pnpm watch`       | Compiles the library in watch mode                                                    |
| `pnpm demo`        | Watches the demo (and the library) and launches the Vite dev server                   |
| `pnpm build:demo`  | Builds the demo production bundle in `demo/dist/`                                     |

## Configuration files

- `Directory.UserConfig.props` is the file where you are expected to put configuration unique to your project.

- `Directory.Packages.props` contains the list of NuGet packages that are used in this project. You can add or remove packages from this file.

    The easiest way to add a package is to use the `dotnet add package` command. It will automatically add the package to all files that need it.

    ```bash
    # For the main project
    dotnet add src/Mibo.Fable package Fable.Core
    # For the demo project
    dotnet add demo package Fable.Core
    ```

- `Directory.Build.props` contains a set of default rules that should works for any project. Only modify it if you know what you are doing. ⚠️

The template pre-configures a lot of things for you via the file `Directory.Build.props`, you should not need to modify it. If you do so, please make sure you know what you are doing.

## Commit convention

This repository is set to generate release and changelog based on the commit history.

To make sure, you follow the convention, it is configured to use [EasyBuild.CommitLinter](https://github.com/easybuild-org/EasyBuild.CommitLinter) to validate the commit messages.

## Run the demo

After a `pnpm install`, run:

```bash
pnpm demo
```

This watches the F# sources and serves the demo with Vite. Open the URL printed by Vite (by default http://localhost:5173); changes to `.fs` files are recompiled by Fable and picked up by the browser.

## Build the demo

To produce the demo production bundle:

```bash
pnpm build:demo
```

The output lands in `demo/dist/` and can be deployed to GitHub Pages or any static host.

Note that the Vite config reads the deploy base URL from the `origin` remote of the git repository, so this requires a git clone with an `origin` remote set.

## Make a release

The release automation that used to live in the `build/` project has been removed; releases are not automated yet.

The `changelog-gen` dotnet tool is still configured in `.config/dotnet-tools.json` and computes the next version from `CHANGELOG.md`:

```bash
dotnet tool restore
dotnet changelog-gen CHANGELOG.md --dry-run
```

You need a NuGet API key to push a package, you can get it from https://www.nuget.org/account/apikeys.
