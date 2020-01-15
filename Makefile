SHELL := /bin/bash
.DEFAULT_GOAL := build
.PHONY: build restore install.ci package publish clean
NUGET_API_KEY ?= ""
NUGET_FEED_URL ?= "https://f.feedz.io/digital-creation/open/nuget"

build: clean restore
	dotnet build

restore:
	./paket.sh restore

install.env:
	wget -q https://packages.microsoft.com/config/ubuntu/19.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
	sudo dpkg -i packages-microsoft-prod.deb
	sudo rm packages-microsoft-prod.deb
	sudo apt-get update | true
	sudo apt-get install -y apt-transport-https
	sudo apt-get update | true
	sudo apt-get install -y dotnet-sdk-3.1

package: clean restore
	dotnet pack -c Release -o ${CURDIR}/.out

publish: package
	dotnet nuget push ./.out/*.nupkg --skip-duplicate -k $(NUGET_API_KEY) -s $(NUGET_FEED_URL)

clean:
	rm -rf ./src/**/obj ./src/**/bin ./.out
