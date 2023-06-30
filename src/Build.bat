IF NOT DEFINED Configuration SET Configuration=Release
MSBuild.exe NotifyIconWpf.sln -t:clean
MSBuild.exe NotifyIconWpf.sln -t:restore -p:RestorePackagesConfig=true
MSBuild.exe NotifyIconWpf.sln -m /property:Configuration=%Configuration%
git add -A
git commit -a --allow-empty-message -m ''
git push