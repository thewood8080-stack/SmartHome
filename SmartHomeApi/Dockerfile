# בנייה והרצה בשני שלבים: תמונת ה-SDK כבדה ונשארת מאחור,
# ובתמונה הסופית יש רק את סביבת הריצה ואת תוצרי ה-publish.
# הקשר הבנייה (build context) הוא תיקיית SmartHomeApi עצמה.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ה-csproj לבדו קודם: כל עוד התלויות לא השתנו, שכבת ה-restore נלקחת מהמטמון
# גם כשקוד המקור משתנה.
COPY SmartHomeApi.csproj ./
RUN dotnet restore SmartHomeApi.csproj

COPY . ./
RUN dotnet publish SmartHomeApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# מגדיר את סביבת הריצה כפרודקשן: כאן מופעלות עוגיות ה-cross-site וההפניה
# ל-HTTPS, ו-Swagger נשאר סגור.
ENV ASPNETCORE_ENVIRONMENT=Production

# ברירת המחדל של התמונה. שרת האירוח מזריק PORT משלו, והקוד מאזין גם עליו.
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartHomeApi.dll"]
