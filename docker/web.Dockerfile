FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/pnpm-lock.yaml* ./
RUN npm install -g pnpm && pnpm install --frozen-lockfile
COPY frontend/ .
RUN pnpm build --configuration production

FROM nginx:alpine AS runtime
COPY --from=build /app/dist/family-os/browser /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
