SHELL := /bin/bash
.ONESHELL:
.DELETE_ON_ERROR:
MAKEFLAGS += --no-builtin-rules

.DEFAULT_GOAL := build
.PHONY: build restore install.env package publish clean
NUGET_API_KEY ?= ""
NUGET_FEED_URL ?= "https://f.feedz.io/digital-creation/open/nuget"

build: clean restore
	dotnet build

restore:
	./paket.sh restore

install.env:
	sudo apt-get update | true
	sudo apt-get install -y apt-transport-https
	sudo apt-get update | true
	sudo apt-get install -y dotnet-sdk-3.1

package: clean restore
	dotnet pack -c Release -o ${CURDIR}/.out

publish: package
	for file in ./.out/*.nupkg ; do \
		dotnet nuget push $${file} --skip-duplicate -k $(NUGET_API_KEY) -s $(NUGET_FEED_URL)
	done

clean:
	rm -rf ./src/**/obj ./src/**/bin ./.out
