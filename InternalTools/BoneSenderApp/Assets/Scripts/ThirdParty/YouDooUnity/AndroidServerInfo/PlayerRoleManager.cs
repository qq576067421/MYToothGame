using System;
using System.Collections.Generic;
using UnityEngine;
using YouDooSDK.Utils;
using static YouDooSDKConstants;

public class PlayerRoleManager : Singleton<PlayerRoleManager>
{

    // 缓存服务端的账号信息
    private AccountInfo _accountInfo;

    public Action UserInfoUpdateCallback;

    /// <summary>
    /// 设置/更新账户信息
    /// </summary>
    public void SetAccountInfo(AccountInfo accountInfo)
    {
        _accountInfo = accountInfo;



        if (accountInfo == null)
        {
            Debug.Log("[PlayerRoleManager] SetAccountInfo: accountInfo 为空");
            return;
        }

        Debug.Log($"[PlayerRoleManager 用户信息 ] SetAccountInfo: accountId={accountInfo.accountId}, username={accountInfo.username}, nickname={accountInfo.nickname}, avatar={accountInfo.avatar}, gender={accountInfo.gender}, birthday={accountInfo.birthday}, birthdayReal={accountInfo.birthdayReal}, intro={accountInfo.intro}, isGuest={accountInfo.isGuest}, verified={accountInfo.verified}, statusParents={accountInfo.statusParents}, statusCurrency={accountInfo.statusCurrency}, usersCount={accountInfo.users?.Count}");

        if (accountInfo.users != null)
        {
            for (int i = 0; i < accountInfo.users.Count; i++)
            {
                RoleInfo role = accountInfo.users[i];
                Debug.Log($"[PlayerRoleManager 用户信息] SetAccountInfo: 角色[{i}] userId={role.userId}, nickname={role.nickname}, avatarId={role.avatarId}, avatarUri={role.avatarUri}, avatarUpdatedAt={role.avatarUpdatedAt}, guardian={role.guardian}, heightMm={role.heightMm}, weightG={role.weightG}, gender={role.gender}, createAt={role.createAt}, facePhotoPath={role.facePhotoPath}, faceDataUpdatedAt={role.faceDataUpdatedAt}");
            }
        }

        if (accountInfo.currencies != null)
        {
            for (int i = 0; i < accountInfo.currencies.Count; i++)
            {
                CurrencyInfo currencie = accountInfo.currencies[i];
                Debug.Log($"[PlayerRoleManager 用户信息] CurrencyInfo: 信息[{i}] userId={currencie.type}, nickname={currencie.currency}");
            }
        }
        UserInfoUpdateCallback?.Invoke();
    }

    /// <summary>
    /// 获取当前的账户信息
    /// </summary>
    public AccountInfo GetAccountInfo()
    {
        return _accountInfo;
    }

    /// <summary>
    /// 根据 userId 获取头像路径
    /// </summary>
    public string GetFacePhotoPathByUserId(long userId)
    {
        if (_accountInfo?.users == null) return null;
        foreach (var user in _accountInfo.users)
        {
            if (user.userId == userId)
            {
                return user.facePhotoPath;
            }
        }
        return null;
    }

    public string GetAvatarUriPathByUserId(long userId)
    {
        if (_accountInfo?.users == null) return null;
        foreach (var user in _accountInfo.users)
        {
            if (user.userId == userId)
            {
                return user.avatarUri;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取给定 userId 数组中 faceDataUpdatedAt 最新的那个 userId
    /// </summary>
    public long GetNewestFaceDataUserId(long[] userIds)
    {
        if (_accountInfo?.users == null || userIds == null || userIds.Length == 0)
        {
            return userIds != null && userIds.Length > 0 ? userIds[0] : 0;
        }

        long newestUserId = userIds[0];
        long maxUpdatedAt = 0;

        foreach (var id in userIds)
        {
            foreach (var user in _accountInfo.users)
            {
                if (user.userId == id)
                {
                    if (user.faceDataUpdatedAt > maxUpdatedAt)
                    {
                        maxUpdatedAt = user.faceDataUpdatedAt;
                        newestUserId = id;
                    }
                    break;
                }
            }
        }

        return newestUserId;
    }

    public void ClearAccountInfo()
    {
        _accountInfo = null;
    }

}
