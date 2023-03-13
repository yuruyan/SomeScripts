/**
���� CommonUITools Release ������Ŀ
 */

import * as fs from "fs"
import * as path from "path"

// ԭ��ĿĿ¼
const releaseDir = "F:/Code/CSharp/CommonUITools/CommonUITools/bin/x64/Release"
const releaseDirs = [
    `${path.join(releaseDir, "net6.0-windows10.0.17763.0")}`,
    `${path.join(releaseDir, "net7.0-windows10.0.17763.0")}`
]
// Ҫ���Ƶ��ļ�
const copyFiles = [
    "CommonTools.xml",
    "CommonUITools.xml",
    "CommonTools.dll",
    "CommonUITools.dll",
]
// Ŀ����Ŀ�ļ���
const targetProjects = [
    "F:/Code/CSharp/IMApp/IMApp.Shared/Lib",
    "F:/Code/CSharp/CommonUtil/CommonUtil.Core/Lib",
    "F:/Code/CSharp/CommonAPI/CommonAPI/Lib"
]
// �汾Ŀ¼
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

