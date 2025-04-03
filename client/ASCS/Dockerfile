# Sử dụng hình ảnh .NET 8 SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Thiết lập thư mục làm việc
WORKDIR /app

# Sao chép file solution và restore các package
COPY *.sln ./
COPY MyASCS.csproj ./

# Restore các package
RUN dotnet restore MyASCS.csproj

# Sao chép toàn bộ mã nguồn và build ứng dụng
COPY . ./
WORKDIR /app
RUN dotnet publish -c Release -o /publish --self-contained false

# Sử dụng hình ảnh với VNC và đầy đủ môi trường GUI
FROM kasmweb/core-ubuntu-focal:1.12.0

USER root

# Cài đặt .NET Runtime
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y apt-transport-https && \
    apt-get update && \
    apt-get install -y dotnet-runtime-8.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Cài đặt các thư viện phụ thuộc cho SkiaSharp và Avalonia
RUN apt-get update && \
    apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libx11-6 \
    libxcursor1 \
    libxext6 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libgl1 \
    libdbus-1-3 \
    libegl1 \
    libglib2.0-0 \
    libharfbuzz0b \
    libicu66 \
    libopengl0 \
    && apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Thiết lập thư mục làm việc
WORKDIR /app

# Sao chép ứng dụng đã build
COPY --from=build /publish ./

# Cấu hình VNC
ENV VNC_PW=password
ENV VNC_RESOLUTION=1920x1080
ENV STARTUP_URL=about:blank
ENV KASM_USER=kasmuser

# Đặt biến môi trường .NET
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV AVALONIA_SCREEN_SCALE_FACTORS=1

# Tạo script khởi động tốt hơn
RUN echo '#!/bin/bash\n\
# Đảm bảo VNC server khởi động trước\n\
/dockerstartup/kasm_default_profile.sh\n\
/dockerstartup/vnc_startup.sh &\n\
\n\
# Đợi VNC server hoàn toàn khởi động\n\
sleep 3\n\
\n\
# Đặt DISPLAY để ứng dụng biết cần render tới đâu\n\
export DISPLAY=:1\n\
\n\
# Chạy ứng dụng Avalonia\n\
cd /app\n\
echo "Đang khởi động ứng dụng..."\n\
dotnet MyASCS.dll\n\
\n\
# Giữ container chạy\n\
#tail -f /dev/null\n\
' > /dockerstartup/custom_startup.sh && chmod +x /dockerstartup/custom_startup.sh

# Thêm quyền chạy cho tất cả file trong thư mục app
RUN chmod -R 755 /app

# Chạy script khởi động
CMD ["/dockerstartup/custom_startup.sh"]
