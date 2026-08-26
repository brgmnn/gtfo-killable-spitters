const fs = require("fs");
const path = require("path");
const { Readable } = require("stream");
const { pipeline } = require("stream/promises");

const run = async ({ github, context, core, io, fetch }) => {
  console.log("Downloading Thunderstore package...");

  const manifest = JSON.parse(fs.readFileSync("manifest.json", "utf8"));
  const { dependencies } = manifest;

  console.log(":: Downloading dependencies");

  await io.mkdirP("./deps");

  for (const dependency of dependencies) {
    const [, team, pkg, version] = dependency.match(
      /^([a-zA-Z0-9_]*)-([a-zA-Z0-9_]*)-([0-9.]*)$/,
    );

    console.log(`   -> Fetching: ${team}-${pkg} @ ${version}`);

    const response = await fetch(
      `https://gcdn.thunderstore.io/live/repository/packages/${dependency}.zip`,
    );

    if (!response.ok) {
      throw new Error(
        `Download failed for ${dependency}.zip: ${response.status} ${response.statusText}`,
      );
    }

    const dest = fs.createWriteStream(
      path.resolve(".", "deps", `${team}-${pkg}.zip`),
    );

    // github-script hands us node-fetch (Node stream body); a native fetch
    // body is a WHATWG stream and needs converting before it can be piped.
    const body =
      typeof response.body.pipe === "function"
        ? response.body
        : Readable.fromWeb(response.body);

    await pipeline(body, dest);
  }
};

module.exports = run;
