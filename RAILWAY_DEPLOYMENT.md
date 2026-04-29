# Railway Deployment Guide - Event Management Portal

## ✅ Prerequisites Completed
- [x] Railway account created
- [x] Configuration files added (`railway.toml`, `nixpacks.toml`)

## 🚀 Step-by-Step Deployment

### Step 1: Push Your Code to GitHub

1. **Initialize Git** (if not already done):
```bash
git init
git add .
git commit -m "Initial commit for Railway deployment"
```

2. **Create a GitHub repository**:
   - Go to https://github.com/new
   - Name it: `event-management-portal`
   - Keep it **Private** (your connection string is in appsettings.json)
   - Don't initialize with README (you already have files)

3. **Push to GitHub**:
```bash
git remote add origin https://github.com/YOUR_USERNAME/event-management-portal.git
git branch -M main
git push -u origin main
```

### Step 2: Deploy on Railway

1. **Go to Railway Dashboard**:
   - Visit: https://railway.app/dashboard

2. **Create New Project**:
   - Click **"New Project"**
   - Select **"Deploy from GitHub repo"**
   - Authorize Railway to access your GitHub
   - Select your `event-management-portal` repository

3. **Railway will automatically**:
   - Detect it's a .NET 8 project
   - Use the `nixpacks.toml` configuration
   - Build and deploy your app

### Step 3: Configure Environment Variables

1. **In Railway Dashboard**, click on your deployed service

2. **Go to "Variables" tab**

3. **Add this environment variable**:
   - **Key**: `ConnectionStrings__DefaultConnection`
   - **Value**: 
   ```
   Host=aws-1-ap-northeast-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.ejzfctwmqdoppfnvqsam;Password=1qJgdCnO4CgEEHoQ;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;Connection Idle Lifetime=30;Connection Pruning Interval=10;Keepalive=10;Tcp Keepalive=true;Command Timeout=120
   ```

4. **Add ASPNETCORE_ENVIRONMENT** (optional but recommended):
   - **Key**: `ASPNETCORE_ENVIRONMENT`
   - **Value**: `Production`

5. **Click "Deploy"** to restart with new variables

### Step 4: Generate Public URL

1. In Railway Dashboard, go to **"Settings"** tab

2. Scroll to **"Networking"** section

3. Click **"Generate Domain"**

4. Railway will give you a URL like: `your-app-name.up.railway.app`

5. **Copy this URL** - this is your live application!

### Step 5: Test Your Deployment

1. Open the Railway-generated URL in your browser

2. You should see your Event Management Portal homepage

3. Test login/registration to verify database connection works

## 🔧 Troubleshooting

### Build Fails
- Check the **"Deployments"** tab for error logs
- Common issue: Missing dependencies (already handled in nixpacks.toml)

### App Crashes on Start
- Check **"Logs"** tab
- Verify environment variable `ConnectionStrings__DefaultConnection` is set correctly
- Ensure Supabase database is accessible

### Database Connection Timeout
- Your connection string already has `Command Timeout=120`
- If still timing out, increase to 180 in Railway environment variable

### 500 Internal Server Error
- Check Railway logs for detailed error
- Verify migrations ran successfully (check logs for "Database connection warmed up")

## 📊 Railway Free Tier Limits

- **500 hours/month** of runtime
- **100 GB bandwidth**
- **1 GB RAM** per service
- **1 GB disk** storage

Your app should fit comfortably within these limits for development/testing.

## 🔄 Updating Your Deployment

Whenever you make code changes:

```bash
git add .
git commit -m "Your update message"
git push
```

Railway will **automatically redeploy** your app!

## 🎉 You're Done!

Your Event Management Portal is now live at:
`https://your-app-name.up.railway.app`

Share this URL with anyone to access your application!

---

## ⚠️ Security Note

After deployment, consider:
1. Moving connection string to Railway environment variables only
2. Removing sensitive data from `appsettings.json`
3. Using `appsettings.Production.json` for production-specific settings
