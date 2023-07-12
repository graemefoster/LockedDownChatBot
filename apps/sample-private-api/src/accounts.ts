import { Response } from 'express';
import { accounts, payments } from './account-data'

const getAccountDetails = async (accountNumber: string, res: Response) => {
    const account = accounts.find(a => a.accountNumber === accountNumber);
    if (account !== undefined) {
        console.log(`Returning account ${accountNumber}`)
        return res.send(account);
    }
    console.log(`Could not find account ${accountNumber}`)
    res.sendStatus(404);
}

const getAccountBalance = async (accountNumber: string, res: Response) => {
    const account = accounts.find(a => a.accountNumber === accountNumber);
    if (account !== undefined) {
        return res.send({
            balance: account.balance,
            asOf: new Date()
        });
    }
    console.log(`Could not find account ${accountNumber}`)
    res.sendStatus(404);
}

const getAccountPayments = async (accountNumber: string, res: Response) => {
    const account = accounts.find(a => a.accountNumber === accountNumber);
    if (account === undefined) {
        return res.sendStatus(404);
    }

    const accountPayments = payments[accountNumber];
    if (accountPayments !== undefined) {
        console.log(`Returning account payments ${accountNumber}`)
        return res.send(accountPayments);
    }
    console.log(`Could not find account payments ${accountNumber}`)
    return res.send([])
}


export default { getAccountDetails, getAccountPayments, getAccountBalance }
