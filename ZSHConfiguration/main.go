package main

import (
	"embed"
	"fmt"
	"io"
	"os"
	"path/filepath"

	"github.com/fatih/color"
)

//go:embed resources/*
var embeddedFiles embed.FS

func main() {
	fmt.Println("Copying resources...")
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
	fmt.Fprintf(os.Stderr, "%s\n", red(err))
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
// dstPath：参数表示复制后的目标文件路径
func CopyResource(resourcesPath string, dstPath string) {
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
	dst := filepath.Join(home, resourcesPath)
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
