project_name = azmetrics
project_dir = ./azmetrics

all: clean build test dotnet_publish_debug dotnet_publish_release

debug:
	dotnet run --project $(project_name) -c Debug

debug_publish: 
	clean build test dotnet_publish_debug

prod_publish: 
	clean build test dotnet_publish_release

clean:
	dotnet clean

build:
	dotnet build

test:
	dotnet test

run:
	dotnet run --project azmetrics

dotnet_publish_debug:
	dotnet publish $(project_dir) -c Debug

dotnet_publish_release:
	dotnet publish $(project_dir) -c Release