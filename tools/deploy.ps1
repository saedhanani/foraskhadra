# غلاف أداة النشر (المرحلة 5.3): يشغّل tools/deploy.cs كتطبيق من ملف واحد (.NET 10) من داخل مجلد tools،
# لأن `dotnet run tools/deploy.cs` من جذر المستودع يبني مشروع الموقع نفسه بدل هذا الملف.
#   .\tools\deploy.ps1                       حزمة + SQL + قائمة تحقق
#   .\tools\deploy.ps1 --verify https://www.foraskhadra.com
#   .\tools\deploy.ps1 --sync --sha <sha>
Push-Location $PSScriptRoot
try {
    & dotnet run deploy.cs -- @args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
