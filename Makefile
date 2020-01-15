SHELL := /bin/bash
.DEFAULT_GOAL := build
.PHONY: build package publish clean
NUGET_API_KEY ?= ""
NUGET_FEED_URL ?= "https://f.feedz.io/digital-creation/open/nuget"

build: clean restore
	dotnet build

restore:
	./paket.sh restore

package: clean restore
	dotnet pack -c Release -o ${CURDIR}/.out

publish: package
	dotnet nuget push ./.out/*.nupkg --skip-duplicate -k $(NUGET_API_KEY) -s $(NUGET_FEED_URL)
	
clean:
	rm -rf ./src/**/obj ./src/**/bin ./.out
