#!/bin/bash

set -e

red="\033[31m"
green="\033[32m"
reset="\033[0m"

# 判断是否安装 zsh
if [ ! -x "$(command -v zsh)" ]; then
    echo -e "${red}zsh 不存在，请先安装 zsh${reset}"
    exit 1
fi
# 判断是否有 unzip 命令
if [ ! -x "$(command -v unzip)" ]; then
    echo -e "${red}unzip 命令不存在，请先安装 unzip${reset}"
    exit 1
fi
# 判断用户是否能执行 sudo
if [ -z "$(sudo -l)" ]; then
    echo -e "${red}当前用户没有 sudo 权限，请先配置 sudo 权限${reset}"
    exit 1
fi

# 解压文件并覆盖
unzip -qo -d ~/.oh-my-zsh Resources/ohmyzsh.zip
echo "unzipped ohmyzsh.zip to '~/.oh-my-zsh'"

cp ~/.oh-my-zsh/templates/zshrc.zsh-template ~/.zshrc
ZSH_PLUGINS=~/.oh-my-zsh/custom/plugins

unzip -qo -d $ZSH_PLUGINS/zsh-syntax-highlighting Resources/zsh-syntax-highlighting.zip
echo "unzipped zsh-syntax-highlighting.zip to '$ZSH_PLUGINS/zsh-syntax-highlighting'"

unzip -qo -d $ZSH_PLUGINS/zsh-autosuggestions Resources/zsh-autosuggestions.zip
echo "unzipped zsh-autosuggestions.zip to '$ZSH_PLUGINS/zsh-autosuggestions'"

unzip -qo -d $ZSH_PLUGINS/zsh-completions Resources/zsh-completions.zip
echo "unzipped zsh-completions.zip to '$ZSH_PLUGINS/zsh-completions'"

# 配置默认 shell 为 zsh
sudo usermod -s /bin/zsh $USER

echo -e "${green}done.${reset}"
