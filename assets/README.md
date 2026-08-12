# Published game resources

Studio Worker writes generated client manifests and browser-ready game resources below `public/versions/` during local development. Temporary generated output is isolated below `work/`, which is not mounted as an nginx document root. Git ignores all generated contents except this file.

The nginx asset server exposes only `public/versions/` at <http://localhost:5300>. Fingerprinted artifact directories are immutable and are tracked by the Studio artifact registry.
