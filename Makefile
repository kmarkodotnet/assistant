.PHONY: up down build test test-all test-integration lint migrate-up init-tls backup restore test-privacy ci zap-scan

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

# TLS inicializálás
init-tls:
	@echo "Initializing TLS CA and certificates..."
	@chmod +x scripts/init-tls-ca.sh && ./scripts/init-tls-ca.sh

# Backup futtatás (manuális)
backup:
	@docker compose run --rm backup /etc/periodic/daily/backup

# Restore
restore:
	@echo "Restore: docker compose run --rm backup /restore.sh <file>"
	@docker compose run --rm -v $(PWD)/scripts/restore.sh:/restore.sh:ro backup /restore.sh $(filter-out $@,$(MAKECMDGOALS))

# Privacy assertion teszt (CI red gate)
test-privacy:
	dotnet test tests/FamilyOs.Infrastructure.Ai.Tests/ --filter "Category=PrivacyAssertion" --no-build

# Full CI
ci:
	dotnet build FamilyOs.sln --no-incremental
	dotnet test FamilyOs.sln
	cd frontend && pnpm build --configuration production

# ZAP scan (needs running stack)
zap-scan:
	@docker run --rm -t zaproxy/zap-stable zap-baseline.py \
	  -t http://localhost:8080 \
	  -r zap-report.html || true
