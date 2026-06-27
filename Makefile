.PHONY: up down build test test-all test-integration lint migrate-up

up:
	docker compose up -d

down:
	docker compose down

build:
	dotnet build FamilyOs.sln

test:
	dotnet test FamilyOs.sln --filter "Category!=Integration"

test-all:
	dotnet test FamilyOs.sln

test-integration:
	dotnet test FamilyOs.sln --filter "Category=Integration"

lint:
	dotnet csharpier --check .

migrate-up:
	dotnet ef database update --project src/FamilyOs.Infrastructure --startup-project src/FamilyOs.Api

fe-install:
	cd frontend && pnpm install

fe-build:
	cd frontend && pnpm build:prod

fe-test:
	cd frontend && pnpm test

fe-lint:
	cd frontend && pnpm lint
