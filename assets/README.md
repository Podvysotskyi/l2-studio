# Published game resources

Studio Worker writes generated client manifests and browser-ready game resources into this directory during local development. Git ignores all generated contents except this file.

The nginx asset server exposes these files at <http://localhost:5300>. Immutable releases belong below `releases/<release-id>/`; never overwrite a published release directory.
