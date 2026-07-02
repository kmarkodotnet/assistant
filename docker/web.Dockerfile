FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ .
RUN npm run build:prod

FROM nginx:1.25-alpine AS runtime
COPY --from=build /app/dist/family-os/browser /usr/share/nginx/html
COPY docker/nginx/family-os.conf /etc/nginx/conf.d/default.conf
EXPOSE 80 443
