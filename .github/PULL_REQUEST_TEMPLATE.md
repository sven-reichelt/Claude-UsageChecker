<!--
  Thanks for contributing. Please read CONTRIBUTING.md first — this project has
  a few conventions that are easy to miss, in particular: everything in the
  repository is written in English, and interface text lives in the language
  files rather than in the source.
-->

## What does this change?

<!-- The problem it solves, not only the code it touches. -->

## Why this way?

<!--
  What alternatives did you weigh, and why did you settle on this one? For this
  project the reasoning matters as much as the change itself — the code base
  documents its decisions.
-->

## How was it verified?

- [ ] `dotnet test` passes
- [ ] Counter-checked new tests: reintroduced the fault and confirmed the test
      actually catches it
- [ ] Anything touching the user interface or the file system was tried in the
      running application, not only in tests

<!--
  The last two are not bureaucracy. Several defects in this project were caught
  only by re-introducing the bug on purpose, or by actually clicking through the
  application — windows that would not open, buttons overflowing their window, a
  cleanup routine that removed the running application.
-->

## Checklist

- [ ] No personal data, tokens or account details anywhere in the diff —
      including test data and screenshots
- [ ] `CHANGELOG.md` updated under *Unreleased*, with its eight translations
- [ ] Analyzer exceptions, if any, are recorded in `.editorconfig` with a
      reason — not scattered as `#pragma` in the source
