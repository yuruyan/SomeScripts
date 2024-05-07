package main

import (
	"archive/zip"
	"bytes"
	"embed"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"runtime/debug"

	"github.com/fatih/color"
	"golang.org/x/text/encoding/simplifiedchinese"
	"golang.org/x/text/transform"
)

//go:embed resources/*
var embeddedFiles embed.FS

func main() {
	fmt.Println("Coping resources...")
	CopyResource("resources/ohmyzsh.zip", "ohmyzsh.zip")
	Unzip("C:/Users/18804/ohmyzsh.zip", "ohmyzsh")
}

// PrintError 函数用于打印错误信息，并根据exit参数决定是否退出程序
//
// 参数：
//
// err：错误信息
//
// exit：是否退出程序
func PrintError(err error, exit bool) {
	red := color.New(color.FgRed).SprintFunc()
	fmt.Fprintf(os.Stderr, "%s\n", red(err.Error()))
	debug.PrintStack()
	if exit {
		os.Exit(1)
	}
}

// CopyResource 函数用于将嵌入的资源文件复制到用户的主目录下
//
// 参数：
//
// resourcesPath：参数表示要复制的资源文件的路径
//
// savePath：参数表示要复制到的文件保存路径，相对于用户主目录
func CopyResource(resourcesPath string, savePath string) {
	// 打开嵌入的资源
	file, err := embeddedFiles.Open(resourcesPath)
	if err != nil {
		PrintError(err, true)
	}
	defer file.Close()
	// copy file to user home path
	home, err := os.UserHomeDir()
	if err != nil {
		PrintError(err, true)
	}
	dst := filepath.Join(home, savePath)
	// 创建文件
	out, err := os.Create(dst)
	if err != nil {
		PrintError(err, true)
	}
	defer out.Close()
	// 写入文件
	_, err = io.Copy(out, file)
	if err != nil {
		PrintError(err, true)
	}
	greenBg := color.New(color.FgGreen).SprintFunc()
	fmt.Println(greenBg(fmt.Sprintf("%s copied to %s", resourcesPath, dst)))
}

// Unzip 函数用于解压缩ZIP文件到指定目录
//
// 参数：
//
// srcFile: ZIP文件路径
//
// saveDir: 解压后的文件保存目录
func Unzip(srcFile string, saveDir string) {
	// 打开ZIP文件
	zipFile, err := zip.OpenReader(srcFile)
	if err != nil {
		PrintError(err, true)
	}
	defer zipFile.Close()

	// 遍历ZIP文件中的文件
	for _, file := range zipFile.File {
		var decodeName string
		if file.Flags == 0 {
			//如果标致位是0  则是默认的本地编码   默认为gbk
			i := bytes.NewReader([]byte(file.Name))
			decoder := transform.NewReader(i, simplifiedchinese.GB18030.NewDecoder())
			content, _ := io.ReadAll(decoder)
			decodeName = string(content)
		} else {
			//如果标志为是 1 << 11也就是 2048  则是utf-8编码
			decodeName = file.Name
		}
		// 创建目标文件路径
		filePath := filepath.Join(saveDir, decodeName)

		// 检查文件路径是否为目录
		if file.FileInfo().IsDir() {
			// 创建目录
			os.MkdirAll(filePath, os.ModePerm)
			continue
		}

		// 创建目标文件的父目录
		dirPath := filepath.Dir(filePath)
		os.MkdirAll(dirPath, os.ModePerm)

		// 打开ZIP文件中的文件
		zipFile, err := file.Open()
		if err != nil {
			PrintError(err, true)
		}
		defer zipFile.Close()

		// 创建目标文件
		outFile, err := os.Create(filePath)
		if err != nil {
			PrintError(err, true)
		}
		defer outFile.Close()

		// 将ZIP文件中的文件内容复制到目标文件
		_, err = io.Copy(outFile, zipFile)
		if err != nil {
			PrintError(err, true)
		}
	}

	greenBg := color.New(color.FgGreen).SprintFunc()
	srcFileAbs, _ := filepath.Abs(srcFile)
	saveDirAbs, _ := filepath.Abs(saveDir)
	fmt.Println(greenBg(fmt.Sprintf("'%s' unzipped to '%s'", srcFileAbs, saveDirAbs)))
}
