import express from 'express'
import accounts from './accounts'


const app = express()

app.set('port', process.env.PORT || 3000);
app.use(express.json())

const router = express.Router()

router.get('/account/:accountNumber/balance', (req, res) => accounts.getAccountBalance(req.params.accountNumber, res));
router.get('/account/:accountNumber/details', (req, res) =>  accounts.getAccountDetails(req.params.accountNumber, res));
router.get('/account/:accountNumber/payments', (req, res) =>  accounts.getAccountPayments(req.params.accountNumber, res));

app.use(router)
app.listen(app.get('port'))

console.log(`listening on port ${app.get('port')}`)
