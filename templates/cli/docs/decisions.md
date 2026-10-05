# Decisions

Newest first. Each entry says what was decided, why, and the evidence. Superseded entries stay, marked as such.

## 2026-10-05: NativeAOT with the XenoAtom stack

The tool publishes as one NativeAOT binary and uses `XenoAtom.CommandLine` for parsing and `XenoAtom.Terminal` (+ `.UI` for the spinner and prompts) for output. All of them publish with zero IL2xxx/IL3xxx warnings, which matters because warnings are errors here. JSON is source-generated, and there is no hosting, DI container or logging framework, which keeps startup fast and the binary small.

Evidence: `dotnet build.cs ci` passes on the template's own CI, publishing the native exe and running the e2e suite against it.
