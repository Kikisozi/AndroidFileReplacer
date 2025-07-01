@echo off
echo 开始上传代码到GitHub...
cd /d %~dp0
git init
git add .
git commit -m "初始提交：AndroidFileReplacer开源项目"
git branch -M main
git remote add origin https://github.com/Kikisozi/AndroidFileReplacer.git
git push -u origin main
echo 上传完成，请在浏览器中检查: https://github.com/Kikisozi/AndroidFileReplacer
pause 