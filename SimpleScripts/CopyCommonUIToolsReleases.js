/**
复制 CommonUITools Release 到各项目
 */

import * as fs from "fs"
import * as path from "path"

// 原项目目录
const releaseDir = "F:/Code/CSharp/CommonUITools/CommonUITools/bin/x64/Release"
const releaseDirs = [
    `${path.join(releaseDir, "net6.0-windows10.0.17763.0")}`,
    `${path.join(releaseDir, "net7.0-windows10.0.17763.0")}`
]
// 要复制的文件
const copyFiles = [
    "CommonTools.xml",
    "CommonUITools.xml",
    "CommonTools.dll",
    "CommonUITools.dll",
]
// 目标项目文件夹
const targetProjects = [
    "F:/Code/CSharp/IMApp/IMApp.Shared/Lib",
    "F:/Code/CSharp/CommonUtil/CommonUtil.Core/Lib",
    "F:/Code/CSharp/CommonAPI/CommonAPI/Lib"
]
// 版本目录
const targetDirs = [
    "Net6",
    "Net7"
]

for (let projDir of targetProjects) {
    // Net Version
    for (var i = 0; i < releaseDirs.length; i++) {
        // Target files
        for (let file of copyFiles) {
            fs.copyFileSync(
                `${path.join(releaseDirs[i], file)}`,
                `${path.join(projDir, targetDirs[i], file)}`
            )
        }
    }
    console.log(`Copy files to ${projDir} done`)
}

