SHELL := /bin/bash
.ONESHELL:
.DELETE_ON_ERROR:
MAKEFLAGS += --no-builtin-rules

.DEFAULT_GOAL := build
.PHONY: build restore install.env package publish clean
NUGET_API_KEY ?= ""
NUGET_FEED_URL ?= "https://api.nuget.org/v3/index.json"

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
ifdef NUGET_URL
ifdef NUGET_KEY
	dotnet nuget push "./.out/*.nupkg" -k $(NUGET_API_KEY) -s $(NUGET_FEED_URL) --skip-duplicate
endif
endif

clean:
	rm -rf ./src/**/obj ./src/**/bin ./.out
