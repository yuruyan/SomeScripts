/**
复制 CommonUITools Release 到各项目
*/

import * as fs from "fs";
import * as path from "path";
import JSON5 from "json5";
import { fileURLToPath } from "url";
// 要复制的文件
const commonToolsFiles = ["CommonTools.xml", "CommonTools.dll"];
const commonUIToolsFiles = ["CommonUITools.xml", "CommonUITools.dll"];
const config = JSON5.parse(
  fs
    .readFileSync(
      path.join(
        path.dirname(fileURLToPath(import.meta.url)),
        "./Resources/CopyCommonUIToolsReleases.json"
      )
    )
    .toString()
);
const releaseRootDir = config["releaseRootDir"];
const releaseVersions = Object.keys(config["releaseVersions"]);
// copy
for (const target of config["targets"]) {
  const destRootDir = target["destRootDir"];
  let versions = target["versions"];
  // not specify 'versions'
  if (typeof versions == "undefined") {
    versions = releaseVersions;
  }
  let versionDirs = target["versionDirs"];
  // not specify 'versionDirs'
  if (typeof versionDirs == "undefined") {
    versionDirs = versions;
  }
  // version
  for (let i = 0; i < versions.length; i++) {
    const version = versions[i];
    const releaseVersionDir = config["releaseVersions"][version];
    // version doesn't exist
    if (typeof releaseVersionDir == "undefined") {
      console.log(`release version '${version}' is invalid`);
      continue;
    }
    // create destination folder
    for (const dir of versionDirs) {
      fs.mkdirSync(path.join(destRootDir, dir), { recursive: true });
    }
    // coping CommonTools
    for (const filename of commonToolsFiles) {
      fs.copyFileSync(
        path.join(releaseRootDir, releaseVersionDir, filename),
        path.join(destRootDir, versionDirs[i], filename)
      );
    }
    if (!target["copyUITools"]) {
      continue;
    }
    // coping CommonUITools
    for (const filename of commonUIToolsFiles) {
      fs.copyFileSync(
        path.join(releaseRootDir, releaseVersionDir, filename),
        path.join(destRootDir, versionDirs[i], filename)
      );
    }
  }
  console.log(`copying ${target["destRootDir"]} done`);
}

await new Promise((resolve, reject) => {
  setTimeout(() => resolve(), 1000);
});
console.log("over");
